namespace Ardayasa.Domain.Entities;

public class ArticleCategory
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public int SortOrder { get; set; }
}
