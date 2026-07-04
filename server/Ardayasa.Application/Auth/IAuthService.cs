using Ardayasa.Application.Common;

namespace Ardayasa.Application.Auth;

public interface IAuthService
{
    /// <summary>Patient self-registration. Sends a verification email; account cannot log in until verified.</summary>
    Task<Result> RegisterPatientAsync(RegisterRequest request, CancellationToken ct = default);

    Task<Result> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    /// <summary>Sends a fresh verification email if the account exists and is unverified. Always succeeds (no enumeration).</summary>
    Task<Result> ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken ct = default);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>Login/link via a Google ID token. Fails with auth.google_not_configured when no client id is set.</summary>
    Task<Result<AuthResponse>> LoginWithGoogleAsync(GoogleLoginRequest request, string? ipAddress, CancellationToken ct = default);

    /// <summary>Rotates the refresh token: the presented token is revoked and a successor is issued.</summary>
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct = default);

    /// <summary>Revokes the presented refresh token. Idempotent.</summary>
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>Invited psychologist sets their password using the one-time invitation token.</summary>
    Task<Result> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken ct = default);

    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
}
