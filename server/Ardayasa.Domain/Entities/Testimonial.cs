namespace Ardayasa.Domain.Entities;

/// <summary>
/// Client testimonial, admin-managed. Shown on the home page and, when linked to a
/// psychologist, on that psychologist's profile page.
/// </summary>
public class Testimonial
{
    public Guid Id { get; set; }

    /// <summary>Display name, typically abbreviated for privacy, e.g. "Alya D.".</summary>
    public required string AuthorName { get; set; }

    /// <summary>Context label shown under the name, e.g. "Klien Konseling Individu".</summary>
    public string? RoleLabel { get; set; }

    public required string Content { get; set; }

    /// <summary>1–5 stars.</summary>
    public int Rating { get; set; } = 5;

    /// <summary>Optional link to a psychologist for profile-page testimonials.</summary>
    public Guid? PsychologistId { get; set; }

    public Psychologist? Psychologist { get; set; }

    public bool IsPublished { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
