using Ardayasa.Application.Auth;
using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Domain;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Email;
using Ardayasa.Infrastructure.Identity;
using Ardayasa.Infrastructure.Options;
using Ardayasa.Infrastructure.Persistence;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ardayasa.Infrastructure.Auth;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    JwtTokenService tokenService,
    IEmailSender emailSender,
    IOptions<GoogleOptions> googleOptions,
    IOptions<AppOptions> appOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly GoogleOptions _google = googleOptions.Value;
    private readonly AppOptions _app = appOptions.Value;

    public async Task<Result> RegisterPatientAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return Result.Failure(AuthErrors.EmailAlreadyRegistered);
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.WhatsAppNumber.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };

        var created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return Result.Failure(ToErrors(created));
        }

        await userManager.AddToRoleAsync(user, Roles.Patient);

        // Email failure must not abort registration — the account already exists,
        // and the user can request a new link via resend-verification.
        try
        {
            await SendVerificationEmailAsync(user, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
        }

        return Result.Success();
    }

    public async Task<Result> ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken ct = default)
    {
        // Always succeed to avoid account enumeration.
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is { IsActive: true, EmailConfirmed: false })
        {
            try
            {
                await SendVerificationEmailAsync(user, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to resend verification email to {Email}", user.Email);
            }
        }

        return Result.Success();
    }

    public async Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidToken);
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        return result.Succeeded ? Result.Success() : Result.Failure(AuthErrors.InvalidToken);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result<AuthResponse>.Failure(AuthErrors.AccountLockedOut);
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure(AuthErrors.AccountInactive);
        }

        if (!user.EmailConfirmed)
        {
            return Result<AuthResponse>.Failure(AuthErrors.EmailNotVerified);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return Result<AuthResponse>.Success(await IssueTokensAsync(user, ipAddress, ct));
    }

    public async Task<Result<AuthResponse>> LoginWithGoogleAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken ct = default)
    {
        if (!_google.IsConfigured)
        {
            return Result<AuthResponse>.Failure(AuthErrors.GoogleNotConfigured);
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [_google.ClientId] });
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Google ID token validation failed");
            return Result<AuthResponse>.Failure(AuthErrors.GoogleTokenInvalid);
        }

        if (payload.EmailVerified != true)
        {
            return Result<AuthResponse>.Failure(AuthErrors.GoogleTokenInvalid);
        }

        var user = await userManager.FindByEmailAsync(payload.Email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = payload.Email,
                Email = payload.Email,
                FullName = string.IsNullOrWhiteSpace(payload.Name) ? payload.Email : payload.Name,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow,
            };

            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded)
            {
                return Result<AuthResponse>.Failure(ToErrors(created));
            }

            await userManager.AddToRoleAsync(user, Roles.Patient);
            await userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
        }
        else
        {
            if (!user.IsActive)
            {
                return Result<AuthResponse>.Failure(AuthErrors.AccountInactive);
            }

            // Google has verified ownership of this address.
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await userManager.UpdateAsync(user);
            }

            var logins = await userManager.GetLoginsAsync(user);
            if (!logins.Any(l => l.LoginProvider == "Google"))
            {
                await userManager.AddLoginAsync(user, new UserLoginInfo("Google", payload.Subject, "Google"));
            }
        }

        return Result<AuthResponse>.Success(await IssueTokensAsync(user, ipAddress, ct));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default)
    {
        var hash = JwtTokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        if (!stored.IsActive)
        {
            // A previously rotated/revoked token was presented again — likely theft.
            // Revoke every active session for this user.
            logger.LogWarning("Refresh token reuse detected for user {UserId}; revoking all sessions", stored.UserId);
            await db.RefreshTokens
                .Where(t => t.UserId == stored.UserId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), ct);
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var user = await userManager.FindByIdAsync(stored.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidRefreshToken);
        }

        var response = await IssueTokensAsync(user, ipAddress, ct);
        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = JwtTokenService.HashRefreshToken(response.RefreshToken);
        await db.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(response);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = JwtTokenService.HashRefreshToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is { RevokedAtUtc: null })
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // Always succeed to avoid account enumeration.
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is { IsActive: true, EmailConfirmed: true })
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var url = BuildWebLink("/atur-ulang-kata-sandi", user.Email!, token);
            var (subject, body) = EmailTemplates.PasswordReset(user.FullName, url);
            await emailSender.SendAsync(user.Email!, subject, body, ct);
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidToken);
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            return Result.Failure(ToErrors(result));
        }

        // A password reset proves control of the mailbox and invalidates existing sessions.
        await RevokeAllSessionsAsync(user.Id, ct);
        return Result.Success();
    }

    public async Task<Result> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Result.Failure(AuthErrors.InvalidToken);
        }

        if (await userManager.HasPasswordAsync(user))
        {
            return Result.Failure(AuthErrors.InvitationAlreadyAccepted);
        }

        // Invitations reuse the password-reset token purpose (single-use, expiring).
        var result = await userManager.ResetPasswordAsync(user, request.Token, request.Password);
        if (!result.Succeeded)
        {
            return Result.Failure(ToErrors(result));
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        return Result.Success();
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.FullName, user.Email!, user.PhoneNumber, [.. roles]);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, string? ipAddress, CancellationToken ct)
    {
        var roles = await userManager.GetRolesAsync(user);
        var (accessToken, expiresIn) = tokenService.CreateAccessToken(user, roles);

        var refreshToken = JwtTokenService.CreateRefreshToken();
        var expiresAtUtc = tokenService.RefreshTokenExpiryUtc();
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = JwtTokenService.HashRefreshToken(refreshToken),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress,
        });
        await db.SaveChangesAsync(ct);

        var dto = new UserDto(user.Id, user.FullName, user.Email!, user.PhoneNumber, [.. roles]);
        return new AuthResponse(dto, accessToken, expiresIn, refreshToken, expiresAtUtc);
    }

    private async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct)
        => await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow), ct);

    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var url = BuildWebLink("/verifikasi-email", user.Email!, token);
        var (subject, body) = EmailTemplates.EmailVerification(user.FullName, url);
        await emailSender.SendAsync(user.Email!, subject, body, ct);
    }

    private string BuildWebLink(string path, string email, string token)
        => $"{_app.WebBaseUrl.TrimEnd('/')}{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";

    private static Error[] ToErrors(IdentityResult result)
        => [.. result.Errors.Select(e => AuthErrors.IdentityError(e.Code, e.Description))];
}
