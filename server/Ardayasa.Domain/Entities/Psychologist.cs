namespace Ardayasa.Domain.Entities;

/// <summary>
/// Psychologist record (created by admin invitation) plus the public profile
/// fields shown on the marketing site (Phase 1).
/// </summary>
public class Psychologist
{
    public Guid Id { get; set; }

    /// <summary>Identity user id. Unique — one psychologist record per account.</summary>
    public Guid UserId { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Professional title/credentials, e.g. "M.Psi., Psikolog".</summary>
    public string? Title { get; set; }

    /// <summary>URL slug for the public profile page, unique.</summary>
    public string? Slug { get; set; }

    /// <summary>Specialization badge, e.g. "Psikolog Klinis Dewasa".</summary>
    public string? Specialization { get; set; }

    /// <summary>Education lines, e.g. "Magister Psikologi Profesi, Universitas Indonesia".</summary>
    public List<string> Education { get; set; } = [];

    /// <summary>Areas of expertise shown as a bullet list.</summary>
    public List<string> Expertise { get; set; } = [];

    public string? Bio { get; set; }

    /// <summary>IFileStorage key of the profile photo.</summary>
    public string? PhotoKey { get; set; }

    /// <summary>
    /// Static practice-schedule text for Phase 1 (one line per entry, e.g.
    /// "Senin 09.00–13.00 WIB"). Replaced by availability-derived data in Phase 2.
    /// </summary>
    public List<string> ScheduleLines { get; set; } = [];

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }
}
