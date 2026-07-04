namespace Ardayasa.Domain.Entities;

/// <summary>
/// A catalog entry from the clinic pricelist. Display-only in Phase 1; becomes the
/// bookable catalog in Phase 2. Pricing is global per service (confirmed 2026-07-04),
/// with separate offline/online prices — null means that mode is not offered.
/// Prices are IDR.
/// </summary>
public class Service
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public ServiceCategory? Category { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Session duration in minutes. Null when not advertised (e.g. screenings).</summary>
    public int? DurationMinutes { get; set; }

    /// <summary>IDR price for in-person sessions; null = not offered in person.</summary>
    public decimal? OfflinePrice { get; set; }

    /// <summary>IDR price for online sessions; null = not offered online.</summary>
    public decimal? OnlinePrice { get; set; }

    /// <summary>Number of sessions included; > 1 for bundles.</summary>
    public int SessionCount { get; set; } = 1;

    /// <summary>Short display note, e.g. "anak + orang tua".</summary>
    public string? Notes { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
