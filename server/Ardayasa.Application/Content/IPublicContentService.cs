using Ardayasa.Application.Common;

namespace Ardayasa.Application.Content;

public record ArticleQuery(string? CategorySlug, string? Search, int Page = 1, int PageSize = 9);

/// <summary>
/// Read-only content for the public marketing site. Anonymous access; only
/// published/active content is ever returned.
/// </summary>
public interface IPublicContentService
{
    Task<IReadOnlyList<PsychologistSummaryDto>> GetPsychologistsAsync(CancellationToken ct = default);

    Task<PsychologistDetailDto?> GetPsychologistAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<ServiceCategoryDto>> GetServiceCatalogAsync(CancellationToken ct = default);

    Task<PagedResult<ArticleListItemDto>> GetArticlesAsync(ArticleQuery query, CancellationToken ct = default);

    Task<ArticleDetailDto?> GetArticleAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<ArticleCategoryDto>> GetArticleCategoriesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<FaqItemDto>> GetFaqAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync(CancellationToken ct = default);
}
