using Ardayasa.Application.Common;

namespace Ardayasa.Infrastructure.Auth;

/// <summary>
/// Stable error codes returned by auth endpoints. The Angular app maps these to
/// Indonesian messages in its translation files.
/// </summary>
public static class AuthErrors
{
    public static readonly Error InvalidCredentials = new("auth.invalid_credentials", "Email or password is incorrect.");
    public static readonly Error EmailNotVerified = new("auth.email_not_verified", "Email address has not been verified.");
    public static readonly Error AccountInactive = new("auth.account_inactive", "Account is deactivated.");
    public static readonly Error AccountLockedOut = new("auth.locked_out", "Account temporarily locked after repeated failures.");
    public static readonly Error EmailAlreadyRegistered = new("auth.email_taken", "An account with this email already exists.");
    public static readonly Error InvalidToken = new("auth.invalid_token", "Token is invalid or has expired.");
    public static readonly Error InvalidRefreshToken = new("auth.invalid_refresh_token", "Refresh token is invalid, expired, or revoked.");
    public static readonly Error GoogleNotConfigured = new("auth.google_not_configured", "Google login is not configured on this server.");
    public static readonly Error GoogleTokenInvalid = new("auth.google_token_invalid", "Google ID token could not be validated.");
    public static readonly Error UserNotFound = new("auth.user_not_found", "No account matches the given identifier.");
    public static readonly Error InvitationAlreadyAccepted = new("auth.invitation_already_accepted", "This invitation has already been used.");

    public static Error IdentityError(string code, string description) => new($"identity.{code}", description);
}
