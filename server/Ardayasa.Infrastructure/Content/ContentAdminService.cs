using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Content;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Content;

public class ContentAdminService(
    AppDbContext db,
    IContentSanitizer sanitizer,
    IAuditLogger audit) : IContentAdminService
{
    // --- Articles ---

    public async Task<IReadOnlyList<AdminArticleDto>> ListArticlesAsync(CancellationToken ct = default)
    {
        var items = await db.Articles
            .AsNoTracking()
            .OrderByDescending(a => a.UpdatedAtUtc)
            .ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<AdminArticleDto?> GetArticleAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Articles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? null : ToDto(a);
    }

    public async Task<Result<AdminArticleDto>> CreateArticleAsync(SaveArticleRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var slugResult = await ResolveArticleSlugAsync(request, existingId: null, ct);
        if (!slugResult.Succeeded)
        {
            return Result<AdminArticleDto>.Failure([.. slugResult.Errors]);
        }

        if (request.CategoryId is { } categoryId
            && !await db.ArticleCategories.AnyAsync(c => c.Id == categoryId, ct))
        {
            return Result<AdminArticleDto>.Failure(ContentErrors.CategoryNotFound);
        }

        var now = DateTime.UtcNow;
        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = slugResult.Value!,
            Excerpt = request.Excerpt?.Trim(),
            ContentHtml = sanitizer.Sanitize(request.ContentHtml),
            FeaturedImageKey = request.FeaturedImageKey,
            CategoryId = request.CategoryId,
            AuthorUserId = actorUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        db.Articles.Add(article);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "article.created", nameof(Article), article.Id.ToString(), new { article.Title }, ct);
        return Result<AdminArticleDto>.Success(ToDto(article));
    }

    public async Task<Result<AdminArticleDto>> UpdateArticleAsync(Guid id, SaveArticleRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
        {
            return Result<AdminArticleDto>.Failure(ContentErrors.NotFound);
        }

        var slugResult = await ResolveArticleSlugAsync(request, existingId: id, ct);
        if (!slugResult.Succeeded)
        {
            return Result<AdminArticleDto>.Failure([.. slugResult.Errors]);
        }

        if (request.CategoryId is { } categoryId
            && !await db.ArticleCategories.AnyAsync(c => c.Id == categoryId, ct))
        {
            return Result<AdminArticleDto>.Failure(ContentErrors.CategoryNotFound);
        }

        article.Title = request.Title.Trim();
        article.Slug = slugResult.Value!;
        article.Excerpt = request.Excerpt?.Trim();
        article.ContentHtml = sanitizer.Sanitize(request.ContentHtml);
        article.FeaturedImageKey = request.FeaturedImageKey;
        article.CategoryId = request.CategoryId;
        article.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "article.updated", nameof(Article), article.Id.ToString(), new { article.Title }, ct);
        return Result<AdminArticleDto>.Success(ToDto(article));
    }

    public async Task<Result> SetArticleStatusAsync(Guid id, bool publish, Guid actorUserId, CancellationToken ct = default)
    {
        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        article.Status = publish ? ArticleStatus.Published : ArticleStatus.Draft;
        article.PublishedAtUtc = publish ? article.PublishedAtUtc ?? DateTime.UtcNow : article.PublishedAtUtc;
        article.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, publish ? "article.published" : "article.unpublished", nameof(Article), id.ToString(), null, ct);
        return Result.Success();
    }

    public async Task<Result> DeleteArticleAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (article is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        db.Articles.Remove(article);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "article.deleted", nameof(Article), id.ToString(), new { article.Title }, ct);
        return Result.Success();
    }

    public async Task<Result<ArticleCategoryDto>> CreateArticleCategoryAsync(SaveArticleCategoryRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var slug = SlugHelper.Generate(request.Name);
        if (await db.ArticleCategories.AnyAsync(c => c.Slug == slug, ct))
        {
            return Result<ArticleCategoryDto>.Failure(ContentErrors.SlugTaken);
        }

        var category = new ArticleCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            SortOrder = request.SortOrder,
        };
        db.ArticleCategories.Add(category);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "article_category.created", nameof(ArticleCategory), category.Id.ToString(), new { category.Name }, ct);
        return Result<ArticleCategoryDto>.Success(new ArticleCategoryDto(category.Id, category.Name, category.Slug));
    }

    public async Task<Result> DeleteArticleCategoryAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var category = await db.ArticleCategories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        if (await db.Articles.AnyAsync(a => a.CategoryId == id, ct))
        {
            return Result.Failure(ContentErrors.CategoryNotEmpty);
        }

        db.ArticleCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "article_category.deleted", nameof(ArticleCategory), id.ToString(), new { category.Name }, ct);
        return Result.Success();
    }

    // --- FAQ ---

    public async Task<IReadOnlyList<AdminFaqItemDto>> ListFaqAsync(CancellationToken ct = default)
        => await db.FaqItems
            .AsNoTracking()
            .OrderBy(f => f.SortOrder)
            .Select(f => new AdminFaqItemDto(f.Id, f.Question, f.AnswerHtml, f.SortOrder, f.IsPublished))
            .ToListAsync(ct);

    public async Task<Result<AdminFaqItemDto>> CreateFaqAsync(SaveFaqItemRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var item = new FaqItem
        {
            Id = Guid.NewGuid(),
            Question = request.Question.Trim(),
            AnswerHtml = sanitizer.Sanitize(request.AnswerHtml),
            SortOrder = request.SortOrder,
            IsPublished = request.IsPublished,
        };
        db.FaqItems.Add(item);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "faq.created", nameof(FaqItem), item.Id.ToString(), new { item.Question }, ct);
        return Result<AdminFaqItemDto>.Success(new AdminFaqItemDto(item.Id, item.Question, item.AnswerHtml, item.SortOrder, item.IsPublished));
    }

    public async Task<Result<AdminFaqItemDto>> UpdateFaqAsync(Guid id, SaveFaqItemRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await db.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (item is null)
        {
            return Result<AdminFaqItemDto>.Failure(ContentErrors.NotFound);
        }

        item.Question = request.Question.Trim();
        item.AnswerHtml = sanitizer.Sanitize(request.AnswerHtml);
        item.SortOrder = request.SortOrder;
        item.IsPublished = request.IsPublished;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "faq.updated", nameof(FaqItem), id.ToString(), new { item.Question }, ct);
        return Result<AdminFaqItemDto>.Success(new AdminFaqItemDto(item.Id, item.Question, item.AnswerHtml, item.SortOrder, item.IsPublished));
    }

    public async Task<Result> DeleteFaqAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await db.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (item is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        db.FaqItems.Remove(item);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "faq.deleted", nameof(FaqItem), id.ToString(), new { item.Question }, ct);
        return Result.Success();
    }

    // --- Testimonials ---

    public async Task<IReadOnlyList<AdminTestimonialDto>> ListTestimonialsAsync(CancellationToken ct = default)
        => await db.Testimonials
            .AsNoTracking()
            .OrderBy(t => t.SortOrder).ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new AdminTestimonialDto(
                t.Id, t.AuthorName, t.RoleLabel, t.Content, t.Rating, t.PsychologistId, t.IsPublished, t.SortOrder))
            .ToListAsync(ct);

    public async Task<Result<AdminTestimonialDto>> CreateTestimonialAsync(SaveTestimonialRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var validation = await ValidateTestimonialAsync(request, ct);
        if (!validation.Succeeded)
        {
            return Result<AdminTestimonialDto>.Failure([.. validation.Errors]);
        }

        var item = new Testimonial
        {
            Id = Guid.NewGuid(),
            AuthorName = request.AuthorName.Trim(),
            RoleLabel = request.RoleLabel?.Trim(),
            Content = request.Content.Trim(),
            Rating = request.Rating,
            PsychologistId = request.PsychologistId,
            IsPublished = request.IsPublished,
            SortOrder = request.SortOrder,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Testimonials.Add(item);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "testimonial.created", nameof(Testimonial), item.Id.ToString(), new { item.AuthorName }, ct);
        return Result<AdminTestimonialDto>.Success(ToDto(item));
    }

    public async Task<Result<AdminTestimonialDto>> UpdateTestimonialAsync(Guid id, SaveTestimonialRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await db.Testimonials.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (item is null)
        {
            return Result<AdminTestimonialDto>.Failure(ContentErrors.NotFound);
        }

        var validation = await ValidateTestimonialAsync(request, ct);
        if (!validation.Succeeded)
        {
            return Result<AdminTestimonialDto>.Failure([.. validation.Errors]);
        }

        item.AuthorName = request.AuthorName.Trim();
        item.RoleLabel = request.RoleLabel?.Trim();
        item.Content = request.Content.Trim();
        item.Rating = request.Rating;
        item.PsychologistId = request.PsychologistId;
        item.IsPublished = request.IsPublished;
        item.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "testimonial.updated", nameof(Testimonial), id.ToString(), new { item.AuthorName }, ct);
        return Result<AdminTestimonialDto>.Success(ToDto(item));
    }

    public async Task<Result> DeleteTestimonialAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var item = await db.Testimonials.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (item is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        db.Testimonials.Remove(item);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "testimonial.deleted", nameof(Testimonial), id.ToString(), new { item.AuthorName }, ct);
        return Result.Success();
    }

    // --- Service catalog ---

    public async Task<IReadOnlyList<AdminServiceCategoryDto>> ListServiceCatalogAsync(CancellationToken ct = default)
    {
        var categories = await db.ServiceCategories
            .AsNoTracking()
            .Include(c => c.Services)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories.Select(c => new AdminServiceCategoryDto(
            c.Id, c.Name, c.Description, c.SortOrder,
            c.Services.OrderBy(s => s.SortOrder).ThenBy(s => s.Name).Select(ToDto).ToList())).ToList();
    }

    public async Task<Result<AdminServiceCategoryDto>> CreateServiceCategoryAsync(SaveServiceCategoryRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var category = new ServiceCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            SortOrder = request.SortOrder,
        };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "service_category.created", nameof(ServiceCategory), category.Id.ToString(), new { category.Name }, ct);
        return Result<AdminServiceCategoryDto>.Success(new AdminServiceCategoryDto(category.Id, category.Name, category.Description, category.SortOrder, []));
    }

    public async Task<Result<AdminServiceCategoryDto>> UpdateServiceCategoryAsync(Guid id, SaveServiceCategoryRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var category = await db.ServiceCategories.Include(c => c.Services).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return Result<AdminServiceCategoryDto>.Failure(ContentErrors.NotFound);
        }

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.SortOrder = request.SortOrder;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "service_category.updated", nameof(ServiceCategory), id.ToString(), new { category.Name }, ct);
        return Result<AdminServiceCategoryDto>.Success(new AdminServiceCategoryDto(
            category.Id, category.Name, category.Description, category.SortOrder,
            category.Services.OrderBy(s => s.SortOrder).Select(ToDto).ToList()));
    }

    public async Task<Result> DeleteServiceCategoryAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var category = await db.ServiceCategories.Include(c => c.Services).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        if (category.Services.Count > 0)
        {
            return Result.Failure(ContentErrors.CategoryNotEmpty);
        }

        db.ServiceCategories.Remove(category);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "service_category.deleted", nameof(ServiceCategory), id.ToString(), new { category.Name }, ct);
        return Result.Success();
    }

    public async Task<Result<AdminServiceDto>> CreateServiceAsync(SaveServiceRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (!await db.ServiceCategories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            return Result<AdminServiceDto>.Failure(ContentErrors.CategoryNotFound);
        }

        var service = new Service
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            DurationMinutes = request.DurationMinutes,
            OfflinePrice = request.OfflinePrice,
            OnlinePrice = request.OnlinePrice,
            SessionCount = Math.Max(1, request.SessionCount),
            Notes = request.Notes?.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
        };
        db.Services.Add(service);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "service.created", nameof(Service), service.Id.ToString(), new { service.Name }, ct);
        return Result<AdminServiceDto>.Success(ToDto(service));
    }

    public async Task<Result<AdminServiceDto>> UpdateServiceAsync(Guid id, SaveServiceRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
        {
            return Result<AdminServiceDto>.Failure(ContentErrors.NotFound);
        }

        if (!await db.ServiceCategories.AnyAsync(c => c.Id == request.CategoryId, ct))
        {
            return Result<AdminServiceDto>.Failure(ContentErrors.CategoryNotFound);
        }

        service.CategoryId = request.CategoryId;
        service.Name = request.Name.Trim();
        service.Description = request.Description?.Trim();
        service.DurationMinutes = request.DurationMinutes;
        service.OfflinePrice = request.OfflinePrice;
        service.OnlinePrice = request.OnlinePrice;
        service.SessionCount = Math.Max(1, request.SessionCount);
        service.Notes = request.Notes?.Trim();
        service.SortOrder = request.SortOrder;
        service.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "service.updated", nameof(Service), id.ToString(), new { service.Name }, ct);
        return Result<AdminServiceDto>.Success(ToDto(service));
    }

    public async Task<Result> DeleteServiceAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var service = await db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (service is null)
        {
            return Result.Failure(ContentErrors.NotFound);
        }

        db.Services.Remove(service);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "service.deleted", nameof(Service), id.ToString(), new { service.Name }, ct);
        return Result.Success();
    }

    // --- Helpers ---

    private async Task<Result<string>> ResolveArticleSlugAsync(SaveArticleRequest request, Guid? existingId, CancellationToken ct)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.Generate(request.Title)
            : SlugHelper.Generate(request.Slug);

        if (slug.Length == 0)
        {
            return Result<string>.Failure(ContentErrors.SlugTaken);
        }

        var taken = await db.Articles.AnyAsync(a => a.Slug == slug && a.Id != existingId, ct);
        return taken ? Result<string>.Failure(ContentErrors.SlugTaken) : Result<string>.Success(slug);
    }

    private async Task<Result> ValidateTestimonialAsync(SaveTestimonialRequest request, CancellationToken ct)
    {
        if (request.Rating is < 1 or > 5)
        {
            return Result.Failure(ContentErrors.InvalidRating);
        }

        if (request.PsychologistId is { } pid
            && !await db.Psychologists.AnyAsync(p => p.Id == pid, ct))
        {
            return Result.Failure(ContentErrors.PsychologistNotFound);
        }

        return Result.Success();
    }

    private static AdminArticleDto ToDto(Article a) => new(
        a.Id, a.Title, a.Slug, a.Excerpt, a.ContentHtml, a.FeaturedImageKey,
        FileUrl.From(a.FeaturedImageKey), a.CategoryId, a.Status.ToString(),
        a.PublishedAtUtc, a.CreatedAtUtc, a.UpdatedAtUtc);

    private static AdminTestimonialDto ToDto(Testimonial t) => new(
        t.Id, t.AuthorName, t.RoleLabel, t.Content, t.Rating, t.PsychologistId, t.IsPublished, t.SortOrder);

    private static AdminServiceDto ToDto(Service s) => new(
        s.Id, s.CategoryId, s.Name, s.Description, s.DurationMinutes,
        s.OfflinePrice, s.OnlinePrice, s.SessionCount, s.Notes, s.SortOrder, s.IsActive);
}
