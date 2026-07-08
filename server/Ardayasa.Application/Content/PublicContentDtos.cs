namespace Ardayasa.Application.Content;

public record PsychologistSummaryDto(
    Guid Id,
    string DisplayName,
    string? Title,
    string? Slug,
    string? Specialization,
    IReadOnlyList<string> Expertise,
    string? PhotoUrl);

public record PsychologistDetailDto(
    Guid Id,
    string DisplayName,
    string? Title,
    string? Slug,
    string? Specialization,
    IReadOnlyList<string> Education,
    IReadOnlyList<string> Expertise,
    string? Bio,
    string? PhotoUrl,
    IReadOnlyList<string> ScheduleLines,
    IReadOnlyList<ScheduleDayDto> Schedule,
    IReadOnlyList<TestimonialDto> Testimonials);

/// <summary>
/// Availability-derived practice schedule (WIB wall-clock times). When empty the
/// client falls back to the static <see cref="PsychologistDetailDto.ScheduleLines"/>;
/// day names are translated client-side (user-facing text never originates server-side).
/// </summary>
public record ScheduleDayDto(DayOfWeek DayOfWeek, IReadOnlyList<ScheduleRangeDto> Ranges);

public record ScheduleRangeDto(TimeOnly StartTime, TimeOnly EndTime);

public record ServiceDto(
    Guid Id,
    string Name,
    string? Description,
    int? DurationMinutes,
    decimal? OfflinePrice,
    decimal? OnlinePrice,
    int SessionCount,
    string? Notes);

public record ServiceCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<ServiceDto> Services);

public record ArticleCategoryDto(Guid Id, string Name, string Slug);

public record ArticleListItemDto(
    string Title,
    string Slug,
    string? Excerpt,
    string? FeaturedImageUrl,
    string? CategoryName,
    string? CategorySlug,
    DateTime? PublishedAtUtc);

public record ArticleDetailDto(
    string Title,
    string Slug,
    string? Excerpt,
    string ContentHtml,
    string? FeaturedImageUrl,
    string? CategoryName,
    string? CategorySlug,
    DateTime? PublishedAtUtc);

public record FaqItemDto(Guid Id, string Question, string AnswerHtml);

public record TestimonialDto(
    Guid Id,
    string AuthorName,
    string? RoleLabel,
    string Content,
    int Rating);
