using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Scheduling;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Scheduling;

/// <summary>
/// Availability is admin-managed only (clinic decision 2026-07-07): mutations are
/// reachable solely from admin endpoints and audit-logged (IDs only); the
/// psychologist dashboard consumes the read-only view.
/// </summary>
public class AvailabilityService(AppDbContext db, IAuditLogger audit) : IAvailabilityService
{
    public async Task<AvailabilityViewDto?> GetOwnAsync(Guid psychologistUserId, CancellationToken ct = default)
    {
        var psychologistId = await db.Psychologists.AsNoTracking()
            .Where(p => p.UserId == psychologistUserId)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct);
        return psychologistId is null ? null : await GetByPsychologistIdAsync(psychologistId.Value, ct);
    }

    public async Task<AvailabilityViewDto?> GetByPsychologistIdAsync(Guid psychologistId, CancellationToken ct = default)
    {
        if (!await db.Psychologists.AnyAsync(p => p.Id == psychologistId, ct))
        {
            return null;
        }

        var rules = await db.AvailabilityRules.AsNoTracking()
            .Where(r => r.PsychologistId == psychologistId)
            .ToListAsync(ct);

        var today = Wib.Today(DateTime.UtcNow);
        var exceptions = await db.AvailabilityExceptions.AsNoTracking()
            .Where(x => x.PsychologistId == psychologistId && x.Date >= today)
            .ToListAsync(ct);

        return new AvailabilityViewDto(
            rules
                .OrderBy(r => ((int)r.DayOfWeek + 6) % 7) // Monday-first week
                .ThenBy(r => r.StartTime)
                .Select(r => new AvailabilityRuleDto(r.Id, r.DayOfWeek, r.StartTime, r.EndTime))
                .ToList(),
            exceptions
                .OrderBy(x => x.Date)
                .ThenBy(x => x.StartTime)
                .Select(ToDto)
                .ToList());
    }

    public async Task<Result<AvailabilityViewDto>> ReplaceRulesAsync(
        Guid psychologistId, ReplaceAvailabilityRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (!await db.Psychologists.AnyAsync(p => p.Id == psychologistId, ct))
        {
            return Result<AvailabilityViewDto>.Failure(SchedulingErrors.PsychologistNotFound);
        }

        var rules = request.Rules ?? [];
        if (rules.Any(r => r.StartTime >= r.EndTime))
        {
            return Result<AvailabilityViewDto>.Failure(SchedulingErrors.InvalidTimeRange);
        }

        foreach (var day in rules.GroupBy(r => r.DayOfWeek))
        {
            var ordered = day.OrderBy(r => r.StartTime).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartTime < ordered[i - 1].EndTime)
                {
                    return Result<AvailabilityViewDto>.Failure(SchedulingErrors.OverlappingRules);
                }
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.AvailabilityRules.Where(r => r.PsychologistId == psychologistId).ExecuteDeleteAsync(ct);
        db.AvailabilityRules.AddRange(rules.Select(r => new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            PsychologistId = psychologistId,
            DayOfWeek = r.DayOfWeek,
            StartTime = r.StartTime,
            EndTime = r.EndTime,
        }));
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "availability.rules_replaced", "Psychologist", psychologistId.ToString(),
            new { ruleCount = rules.Count }, ct);
        await tx.CommitAsync(ct);

        return Result<AvailabilityViewDto>.Success((await GetByPsychologistIdAsync(psychologistId, ct))!);
    }

    public async Task<Result<AvailabilityExceptionDto>> AddExceptionAsync(
        Guid psychologistId, AddAvailabilityExceptionRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (!await db.Psychologists.AnyAsync(p => p.Id == psychologistId, ct))
        {
            return Result<AvailabilityExceptionDto>.Failure(SchedulingErrors.PsychologistNotFound);
        }

        // Extra windows need explicit times; a Block may omit both to block the whole day.
        var hasTimes = request.StartTime is not null && request.EndTime is not null;
        if (request.Kind == AvailabilityExceptionKind.Extra && !hasTimes)
        {
            return Result<AvailabilityExceptionDto>.Failure(SchedulingErrors.ExceptionTimesRequired);
        }

        if (request.StartTime is not null != request.EndTime is not null)
        {
            return Result<AvailabilityExceptionDto>.Failure(SchedulingErrors.ExceptionTimesRequired);
        }

        if (hasTimes && request.StartTime >= request.EndTime)
        {
            return Result<AvailabilityExceptionDto>.Failure(SchedulingErrors.InvalidTimeRange);
        }

        if (request.Date < Wib.Today(DateTime.UtcNow))
        {
            return Result<AvailabilityExceptionDto>.Failure(SchedulingErrors.ExceptionDateInPast);
        }

        var exception = new AvailabilityException
        {
            Id = Guid.NewGuid(),
            PsychologistId = psychologistId,
            Date = request.Date,
            Kind = request.Kind,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
        };
        db.AvailabilityExceptions.Add(exception);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "availability.exception_added", "Psychologist", psychologistId.ToString(),
            new { exception.Date, kind = exception.Kind.ToString() }, ct);

        return Result<AvailabilityExceptionDto>.Success(ToDto(exception));
    }

    public async Task<Result> RemoveExceptionAsync(
        Guid psychologistId, Guid exceptionId, Guid actorUserId, CancellationToken ct = default)
    {
        var exception = await db.AvailabilityExceptions
            .FirstOrDefaultAsync(x => x.Id == exceptionId && x.PsychologistId == psychologistId, ct);
        if (exception is null)
        {
            return Result.Failure(SchedulingErrors.ExceptionNotFound);
        }

        db.AvailabilityExceptions.Remove(exception);
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "availability.exception_removed", "Psychologist", psychologistId.ToString(),
            new { exception.Date, kind = exception.Kind.ToString() }, ct);
        return Result.Success();
    }

    private static AvailabilityExceptionDto ToDto(AvailabilityException x)
        => new(x.Id, x.Date, x.Kind, x.StartTime, x.EndTime);
}
