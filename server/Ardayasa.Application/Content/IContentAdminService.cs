using Ardayasa.Application.Common;

namespace Ardayasa.Application.Content;

/// <summary>
/// Admin CRUD for marketing-site content (articles, FAQ, testimonials, service catalog).
/// All rich-text HTML is sanitized before persisting. Every mutation is audit-logged
/// with the acting admin's user id.
/// </summary>
public interface IContentAdminService
{
    // Articles
    Task<IReadOnlyList<AdminArticleDto>> ListArticlesAsync(CancellationToken ct = default);
    Task<AdminArticleDto?> GetArticleAsync(Guid id, CancellationToken ct = default);
    Task<Result<AdminArticleDto>> CreateArticleAsync(SaveArticleRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result<AdminArticleDto>> UpdateArticleAsync(Guid id, SaveArticleRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result> SetArticleStatusAsync(Guid id, bool publish, Guid actorUserId, CancellationToken ct = default);
    Task<Result> DeleteArticleAsync(Guid id, Guid actorUserId, CancellationToken ct = default);
    Task<Result<ArticleCategoryDto>> CreateArticleCategoryAsync(SaveArticleCategoryRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result> DeleteArticleCategoryAsync(Guid id, Guid actorUserId, CancellationToken ct = default);

    // FAQ
    Task<IReadOnlyList<AdminFaqItemDto>> ListFaqAsync(CancellationToken ct = default);
    Task<Result<AdminFaqItemDto>> CreateFaqAsync(SaveFaqItemRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result<AdminFaqItemDto>> UpdateFaqAsync(Guid id, SaveFaqItemRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result> DeleteFaqAsync(Guid id, Guid actorUserId, CancellationToken ct = default);

    // Testimonials
    Task<IReadOnlyList<AdminTestimonialDto>> ListTestimonialsAsync(CancellationToken ct = default);
    Task<Result<AdminTestimonialDto>> CreateTestimonialAsync(SaveTestimonialRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result<AdminTestimonialDto>> UpdateTestimonialAsync(Guid id, SaveTestimonialRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result> DeleteTestimonialAsync(Guid id, Guid actorUserId, CancellationToken ct = default);

    // Service catalog
    Task<IReadOnlyList<AdminServiceCategoryDto>> ListServiceCatalogAsync(CancellationToken ct = default);
    Task<Result<AdminServiceCategoryDto>> CreateServiceCategoryAsync(SaveServiceCategoryRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result<AdminServiceCategoryDto>> UpdateServiceCategoryAsync(Guid id, SaveServiceCategoryRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result> DeleteServiceCategoryAsync(Guid id, Guid actorUserId, CancellationToken ct = default);
    Task<Result<AdminServiceDto>> CreateServiceAsync(SaveServiceRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result<AdminServiceDto>> UpdateServiceAsync(Guid id, SaveServiceRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Result> DeleteServiceAsync(Guid id, Guid actorUserId, CancellationToken ct = default);
}
