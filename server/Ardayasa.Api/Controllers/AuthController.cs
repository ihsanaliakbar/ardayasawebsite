using Ardayasa.Application.Auth;
using Ardayasa.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ardayasa.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private const string RefreshCookieName = "ardayasa_refresh";

    /// <summary>Response body for token-issuing endpoints; the refresh token travels only in the cookie.</summary>
    public record TokenResponse(UserDto User, string AccessToken, int ExpiresInSeconds);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
        => ToActionResult(await authService.RegisterPatientAsync(request, ct));

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
        => ToActionResult(await authService.VerifyEmailAsync(request, ct));

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerification(ResendVerificationRequest request, CancellationToken ct)
        => ToActionResult(await authService.ResendVerificationEmailAsync(request, ct));

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        => TokenActionResult(await authService.LoginAsync(request, ClientIp(), ct));

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> Google(GoogleLoginRequest request, CancellationToken ct)
        => TokenActionResult(await authService.LoginWithGoogleAsync(request, ClientIp(), ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        return TokenActionResult(await authService.RefreshAsync(refreshToken, ClientIp(), ct));
    }

    [HttpPost("logout")]
    [AllowAnonymous] // must work even with an expired access token; operates on the cookie only
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshCookieName];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await authService.LogoutAsync(refreshToken, ct);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
        => ToActionResult(await authService.ForgotPasswordAsync(request, ct));

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
        => ToActionResult(await authService.ResetPasswordAsync(request, ct));

    [HttpPost("accept-invitation")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationRequest request, CancellationToken ct)
        => ToActionResult(await authService.AcceptInvitationAsync(request, ct));

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst("sub")?.Value, out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetUserAsync(userId, ct);
        return user is null ? Unauthorized() : Ok(user);
    }

    private IActionResult TokenActionResult(Result<AuthResponse> result)
    {
        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        var auth = result.Value!;
        SetRefreshCookie(auth.RefreshToken, auth.RefreshTokenExpiresAtUtc);
        return Ok(new TokenResponse(auth.User, auth.AccessToken, auth.ExpiresInSeconds));
    }

    private IActionResult ToActionResult(Result result)
    {
        if (result.Succeeded)
        {
            return NoContent();
        }

        var body = new { errors = result.Errors };
        return result.Errors.Any(e => UnauthorizedCodes.Contains(e.Code))
            ? Unauthorized(body)
            : BadRequest(body);
    }

    private static readonly string[] UnauthorizedCodes =
    [
        "auth.invalid_credentials",
        "auth.locked_out",
        "auth.invalid_refresh_token",
        "auth.account_inactive",
    ];

    private void SetRefreshCookie(string token, DateTime expiresUtc)
        => Response.Cookies.Append(RefreshCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            Expires = expiresUtc,
        });

    private void ClearRefreshCookie()
        => Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
        });

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
