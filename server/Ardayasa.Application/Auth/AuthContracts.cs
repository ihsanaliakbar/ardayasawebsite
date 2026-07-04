namespace Ardayasa.Application.Auth;

public record RegisterRequest(string FullName, string Email, string WhatsAppNumber, string Password);

public record LoginRequest(string Email, string Password);

public record GoogleLoginRequest(string IdToken);

public record VerifyEmailRequest(string Email, string Token);

public record ResendVerificationRequest(string Email);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record AcceptInvitationRequest(string Email, string Token, string Password);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string? WhatsAppNumber,
    IReadOnlyList<string> Roles);

/// <summary>
/// Full auth payload produced by the service. The API layer moves
/// <see cref="RefreshToken"/> into an httpOnly cookie and never returns it in the body.
/// </summary>
public record AuthResponse(
    UserDto User,
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);
