namespace Ardayasa.Domain.Entities;

/// <summary>
/// Server-side record of an issued refresh token. Only the SHA-256 hash of the
/// token is stored; the raw value lives solely in the httpOnly cookie.
/// Rotation: on use, the token is revoked and ReplacedByTokenHash points at its successor.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string TokenHash { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedByIp { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}
