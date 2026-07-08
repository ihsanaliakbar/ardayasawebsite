using Ardayasa.Application.Common;
using Ardayasa.Application.Content;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Content;

public class PublicContentService(AppDbContext db) : IPublicContentService
{
    private const int MaxPageSize = 24;

    public async Task<IReadOnlyList<PsychologistSummaryDto>> GetPsychologistsAsync(CancellationToken ct = default)
    {
        var items = await db.Psychologists
            .AsNoTracking()
            .Where(p => p.IsActive && p.Slug != null)
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.DisplayName)
            .ToListAsync(ct);

        return items
            .Select(p => new PsychologistSummaryDto(
                p.Id, p.DisplayName, p.Title, p.Slug, p.Specialization, p.Expertise, FileUrl.From(p.PhotoKey)))
            .ToList();
    }

    public async Task<PsychologistDetailDto?> GetPsychologistAsync(string slug, CancellationToken ct = default)
    {
        var p = await db.Psychologists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive, ct);
        if (p is null)
        {
            return null;
        }

        var testimonials = await db.Testimonials
            .AsNoTracking()
            .Where(t => t.IsPublished && t.PsychologistId == p.Id)
            .OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new TestimonialDto(t.Id, t.AuthorName, t.RoleLabel, t.Content, t.Rating))
            .ToListAsync(ct);

        // Live availability replaces the static Phase 1 schedule text once rules
        // exist; the client falls back to ScheduleLines while Schedule is empty.
        var schedule = (await db.AvailabilityRules
                .AsNoTracking()
                .Where(r => r.PsychologistId == p.Id)
                .ToListAsync(ct))
            .GroupBy(r => r.DayOfWeek)
            .OrderBy(g => ((int)g.Key + 6) % 7) // Monday-first week
            .Select(g => new ScheduleDayDto(
                g.Key,
                g.OrderBy(r => r.StartTime)
                    .Select(r => new ScheduleRangeDto(r.StartTime, r.EndTime))
                    .ToList()))
            .ToList();

        return new PsychologistDetailDto(
            p.Id, p.DisplayName, p.Title, p.Slug, p.Specialization,
            p.Education, p.Expertise, p.Bio, FileUrl.From(p.PhotoKey), p.ScheduleLines, schedule, testimonials);
    }

    public async Task<IReadOnlyList<ServiceCategoryDto>> GetServiceCatalogAsync(CancellationToken ct = default)
    {
        var categories = await db.ServiceCategories
            .AsNoTracking()
            .Include(c => c.Services.Where(s => s.IsActive))
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories
            .Select(c => new ServiceCategoryDto(
                c.Id, c.Name, c.Description,
                c.Services
                    .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
                    .Select(s => new ServiceDto(
                        s.Id, s.Name, s.Description, s.DurationMinutes,
                        s.OfflinePrice, s.OnlinePrice, s.SessionCount, s.Notes))
                    .ToList()))
            .Where(c => c.Services.Count > 0)
            .ToList();
    }

    public async Task<PagedResult<ArticleListItemDto>> GetArticlesAsync(ArticleQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);

        var articles = db.Articles
            .AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published);

        if (!string.IsNullOrWhiteSpace(query.CategorySlug))
        {
            articles = articles.Where(a => a.Category != null && a.Category.Slug == query.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Portable case-insensitive match (tests run on SQLite, prod on Postgres).
            var term = query.Search.Trim().ToLower();
            articles = articles.Where(a =>
                a.Title.ToLower().Contains(term)
                || (a.Excerpt != null && a.Excerpt.ToLower().Contains(term)));
        }

        var total = await articles.CountAsync(ct);
        var items = await articles
            .OrderByDescending(a => a.PublishedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Title, a.Slug, a.Excerpt, a.FeaturedImageKey, a.PublishedAtUtc,
                CategoryName = a.Category != null ? a.Category.Name : null,
                CategorySlug = a.Category != null ? a.Category.Slug : null,
            })
            .ToListAsync(ct);

        return new PagedResult<ArticleListItemDto>(
            items.Select(a => new ArticleListItemDto(
                a.Title, a.Slug, a.Excerpt, FileUrl.From(a.FeaturedImageKey),
                a.CategoryName, a.CategorySlug, a.PublishedAtUtc)).ToList(),
            total, page, pageSize);
    }

    public async Task<ArticleDetailDto?> GetArticleAsync(string slug, CancellationToken ct = default)
    {
        var a = await db.Articles
            .AsNoTracking()
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Slug == slug && x.Status == ArticleStatus.Published, ct);

        return a is null
            ? null
            : new ArticleDetailDto(
                a.Title, a.Slug, a.Excerpt, a.ContentHtml, FileUrl.From(a.FeaturedImageKey),
                a.Category?.Name, a.Category?.Slug, a.PublishedAtUtc);
    }

    public async Task<IReadOnlyList<ArticleCategoryDto>> GetArticleCategoriesAsync(CancellationToken ct = default)
        => await db.ArticleCategories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new ArticleCategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FaqItemDto>> GetFaqAsync(CancellationToken ct = default)
        => await db.FaqItems
            .AsNoTracking()
            .Where(f => f.IsPublished)
            .OrderBy(f => f.SortOrder)
            .Select(f => new FaqItemDto(f.Id, f.Question, f.AnswerHtml))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync(CancellationToken ct = default)
        => await db.Testimonials
            .AsNoTracking()
            .Where(t => t.IsPublished)
            .OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new TestimonialDto(t.Id, t.AuthorName, t.RoleLabel, t.Content, t.Rating))
            .ToListAsync(ct);
}
