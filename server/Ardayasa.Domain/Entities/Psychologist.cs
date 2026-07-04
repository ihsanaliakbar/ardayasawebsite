namespace Ardayasa.Domain.Entities;

/// <summary>
/// Minimal psychologist record for Phase 0 (created by admin invitation).
/// Public profile fields (photo, bio, expertise, testimonials) arrive in Phase 1.
/// </summary>
public class Psychologist
{
    public Guid Id { get; set; }

    /// <summary>Identity user id. Unique — one psychologist record per account.</summary>
    public Guid UserId { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Professional title/credentials, e.g. "M.Psi., Psikolog".</summary>
    public string? Title { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
