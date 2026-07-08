using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Scheduling;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PsychologistServiceEntity = Ardayasa.Domain.Entities.PsychologistService;

namespace Ardayasa.Infrastructure.Scheduling;

/// <summary>
/// Admin-managed psychologist↔service mapping (clinic decision 2026-07-07).
/// Only "bookable" catalog services participate: single-session, with a duration
/// and at least one mode price — bundles and non-durational services are booked
/// via WhatsApp in v1.
/// </summary>
public class PsychologistServiceMappingService(AppDbContext db, IAuditLogger audit) : IPsychologistServiceMapping
{
    public async Task<IReadOnlyList<PsychologistServiceMapDto>?> GetForPsychologistAsync(
        Guid psychologistId, CancellationToken ct = default)
    {
        if (!await db.Psychologists.AnyAsync(p => p.Id == psychologistId, ct))
        {
            return null;
        }

        var enabled = await db.PsychologistServices.AsNoTracking()
            .Where(m => m.PsychologistId == psychologistId)
            .Select(m => m.ServiceId)
            .ToListAsync(ct);

        return await BookableServices(db)
            .OrderBy(s => s.Category!.SortOrder)
            .ThenBy(s => s.SortOrder)
            .Select(s => new PsychologistServiceMapDto(
                s.Id, s.Name, s.Category!.Name, s.DurationMinutes, s.OfflinePrice, s.OnlinePrice,
                enabled.Contains(s.Id)))
            .ToListAsync(ct);
    }

    public async Task<Result> ReplaceAsync(
        Guid psychologistId, ReplacePsychologistServicesRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (!await db.Psychologists.AnyAsync(p => p.Id == psychologistId, ct))
        {
            return Result.Failure(SchedulingErrors.PsychologistNotFound);
        }

        var requested = (request.ServiceIds ?? []).Distinct().ToList();
        var bookableIds = await BookableServices(db).Select(s => s.Id).ToListAsync(ct);
        if (requested.Except(bookableIds).Any())
        {
            return Result.Failure(SchedulingErrors.UnknownServiceIds);
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.PsychologistServices.Where(m => m.PsychologistId == psychologistId).ExecuteDeleteAsync(ct);
        db.PsychologistServices.AddRange(requested.Select(serviceId => new PsychologistServiceEntity
        {
            Id = Guid.NewGuid(),
            PsychologistId = psychologistId,
            ServiceId = serviceId,
        }));
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "psychologist.services_replaced", "Psychologist", psychologistId.ToString(),
            new { serviceCount = requested.Count }, ct);
        await tx.CommitAsync(ct);

        return Result.Success();
    }

    /// <summary>Directly bookable in v1: active, single session, has a duration and at least one mode price.</summary>
    internal static IQueryable<Domain.Entities.Service> BookableServices(AppDbContext db)
        => db.Services.AsNoTracking()
            .Where(s => s.IsActive
                        && s.SessionCount == 1
                        && s.DurationMinutes != null
                        && (s.OfflinePrice != null || s.OnlinePrice != null));
}
