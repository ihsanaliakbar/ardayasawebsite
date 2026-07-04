namespace Ardayasa.Application.Content;

// --- Articles ---

public record SaveArticleRequest(
    string Title,
    string? Slug,
    string? Excerpt,
    string ContentHtml,
    Guid? CategoryId,
    string? FeaturedImageKey);

public record AdminArticleDto(
    Guid Id,
    string Title,
    string Slug,
    string? Excerpt,
    string ContentHtml,
    string? FeaturedImageKey,
    string? FeaturedImageUrl,
    Guid? CategoryId,
    string Status,
    DateTime? PublishedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record SaveArticleCategoryRequest(string Name, int SortOrder);

// --- FAQ ---

public record SaveFaqItemRequest(string Question, string AnswerHtml, int SortOrder, bool IsPublished);

public record AdminFaqItemDto(Guid Id, string Question, string AnswerHtml, int SortOrder, bool IsPublished);

// --- Testimonials ---

public record SaveTestimonialRequest(
    string AuthorName,
    string? RoleLabel,
    string Content,
    int Rating,
    Guid? PsychologistId,
    bool IsPublished,
    int SortOrder);

public record AdminTestimonialDto(
    Guid Id,
    string AuthorName,
    string? RoleLabel,
    string Content,
    int Rating,
    Guid? PsychologistId,
    bool IsPublished,
    int SortOrder);

// --- Service catalog ---

public record SaveServiceCategoryRequest(string Name, string? Description, int SortOrder);

public record SaveServiceRequest(
    Guid CategoryId,
    string Name,
    string? Description,
    int? DurationMinutes,
    decimal? OfflinePrice,
    decimal? OnlinePrice,
    int SessionCount,
    string? Notes,
    int SortOrder,
    bool IsActive);

public record AdminServiceDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    int? DurationMinutes,
    decimal? OfflinePrice,
    decimal? OnlinePrice,
    int SessionCount,
    string? Notes,
    int SortOrder,
    bool IsActive);

public record AdminServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    IReadOnlyList<AdminServiceDto> Services);
