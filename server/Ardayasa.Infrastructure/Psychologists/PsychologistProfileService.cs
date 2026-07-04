using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Psychologists;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Content;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Psychologists;

public class PsychologistProfileService(
    AppDbContext db,
    IFileStorage files,
    IAuditLogger audit) : IPsychologistProfileService
{
    public async Task<PsychologistProfileDto?> GetOwnAsync(Guid userId, CancellationToken ct = default)
    {
        var p = await db.Psychologists.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<PsychologistProfileDto?> GetByIdAsync(Guid psychologistId, CancellationToken ct = default)
    {
        var p = await db.Psychologists.AsNoTracking().FirstOrDefaultAsync(x => x.Id == psychologistId, ct);
        return p is null ? null : ToDto(p);
    }

    public async Task<Result<PsychologistProfileDto>> UpdateOwnAsync(Guid userId, UpdatePsychologistProfileRequest request, CancellationToken ct = default)
    {
        var p = await db.Psychologists.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (p is null)
        {
            return Result<PsychologistProfileDto>.Failure(ContentErrors.NotFound);
        }

        await ApplyAsync(p, request, isAdmin: false, ct);
        await db.SaveChangesAsync(ct);
        return Result<PsychologistProfileDto>.Success(ToDto(p));
    }

    public async Task<Result<PsychologistProfileDto>> UpdateAsync(Guid psychologistId, UpdatePsychologistProfileRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var p = await db.Psychologists.FirstOrDefaultAsync(x => x.Id == psychologistId, ct);
        if (p is null)
        {
            return Result<PsychologistProfileDto>.Failure(ContentErrors.NotFound);
        }

        await ApplyAsync(p, request, isAdmin: true, ct);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "psychologist.profile_updated", nameof(Psychologist), p.Id.ToString(), new { p.DisplayName }, ct);
        return Result<PsychologistProfileDto>.Success(ToDto(p));
    }

    public async Task<Result<PsychologistProfileDto>> SetOwnPhotoAsync(Guid userId, Stream content, string fileName, CancellationToken ct = default)
    {
        var p = await db.Psychologists.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        return p is null
            ? Result<PsychologistProfileDto>.Failure(ContentErrors.NotFound)
            : await SetPhotoCoreAsync(p, content, fileName, ct);
    }

    public async Task<Result<PsychologistProfileDto>> SetPhotoAsync(Guid psychologistId, Stream content, string fileName, Guid actorUserId, CancellationToken ct = default)
    {
        var p = await db.Psychologists.FirstOrDefaultAsync(x => x.Id == psychologistId, ct);
        if (p is null)
        {
            return Result<PsychologistProfileDto>.Failure(ContentErrors.NotFound);
        }

        var result = await SetPhotoCoreAsync(p, content, fileName, ct);
        if (result.Succeeded)
        {
            await audit.LogAsync(actorUserId, "psychologist.photo_updated", nameof(Psychologist), p.Id.ToString(), null, ct);
        }

        return result;
    }

    private async Task<Result<PsychologistProfileDto>> SetPhotoCoreAsync(Psychologist p, Stream content, string fileName, CancellationToken ct)
    {
        string key;
        try
        {
            key = await files.SaveAsync(content, fileName, ct);
        }
        catch (InvalidOperationException)
        {
            return Result<PsychologistProfileDto>.Failure(ContentErrors.InvalidFile);
        }

        var oldKey = p.PhotoKey;
        p.PhotoKey = key;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(oldKey))
        {
            await files.DeleteAsync(oldKey, ct);
        }

        return Result<PsychologistProfileDto>.Success(ToDto(p));
    }

    private async Task ApplyAsync(Psychologist p, UpdatePsychologistProfileRequest request, bool isAdmin, CancellationToken ct)
    {
        p.DisplayName = request.DisplayName.Trim();
        p.Title = request.Title?.Trim();
        p.Specialization = request.Specialization?.Trim();
        p.Education = CleanList(request.Education);
        p.Expertise = CleanList(request.Expertise);
        p.Bio = request.Bio?.Trim();
        p.ScheduleLines = CleanList(request.ScheduleLines);

        if (isAdmin)
        {
            p.DisplayOrder = request.DisplayOrder ?? p.DisplayOrder;
            p.IsActive = request.IsActive ?? p.IsActive;
        }

        // The public profile page needs a slug; generate one from the name on first
        // profile save and keep it stable afterwards (URLs shouldn't churn on rename).
        if (string.IsNullOrEmpty(p.Slug))
        {
            p.Slug = await GenerateUniqueSlugAsync(p.DisplayName, ct);
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string displayName, CancellationToken ct)
    {
        var baseSlug = SlugHelper.Generate(displayName);
        var slug = baseSlug;
        var i = 2;
        while (await db.Psychologists.AnyAsync(x => x.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{i++}";
        }

        return slug;
    }

    private static List<string> CleanList(IReadOnlyList<string> items)
        => items.Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

    private static PsychologistProfileDto ToDto(Psychologist p) => new(
        p.Id, p.DisplayName, p.Title, p.Slug, p.Specialization,
        p.Education, p.Expertise, p.Bio, FileUrl.From(p.PhotoKey),
        p.ScheduleLines, p.DisplayOrder, p.IsActive);
}
