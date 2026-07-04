namespace Ardayasa.Domain.Entities;

public enum ArticleStatus
{
    Draft = 0,
    Published = 1,
}

/// <summary>
/// Blog article authored by admins via the TipTap editor. Public detail pages are
/// server-rendered for SEO; only Published articles are ever exposed publicly.
/// </summary>
public class Article
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    /// <summary>URL slug, unique. Generated from the title, editable by admin.</summary>
    public required string Slug { get; set; }

    /// <summary>Short teaser used in lists and meta description.</summary>
    public string? Excerpt { get; set; }

    /// <summary>Sanitized TipTap HTML output.</summary>
    public required string ContentHtml { get; set; }

    /// <summary>IFileStorage key of the featured image, if any.</summary>
    public string? FeaturedImageKey { get; set; }

    public Guid? CategoryId { get; set; }

    public ArticleCategory? Category { get; set; }

    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;

    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>Identity user id of the author (admin).</summary>
    public Guid AuthorUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
