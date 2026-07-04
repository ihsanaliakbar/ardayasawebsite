namespace Ardayasa.Domain.Entities;

/// <summary>
/// Pricelist grouping, e.g. "Konseling", "Asesmen", "Psikoterapi".
/// </summary>
public class ServiceCategory
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public List<Service> Services { get; set; } = [];
}
