using Ardayasa.Application.Common;
using Ardayasa.Application.Patients;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Patients;

/// <summary>
/// NOTE: logbook actions are deliberately NOT audit-logged. The audit log is
/// admin-readable, and admin must not learn that entries exist at all — an
/// IDs-only audit row would already leak that.
/// </summary>
public class LogbookService(AppDbContext db) : ILogbookService
{
    public async Task<IReadOnlyList<LogbookEntryDto>?> ListAsync(Guid psychologistUserId, Guid patientUserId, CancellationToken ct = default)
    {
        var callerPsychologistId = await AssignedPsychologistIdAsync(psychologistUserId, patientUserId, ct);
        if (callerPsychologistId is null)
        {
            return null;
        }

        var entries = await db.LogbookEntries.AsNoTracking()
            .Where(e => e.PatientUserId == patientUserId)
            .OrderByDescending(e => e.SessionDate)
            .ThenByDescending(e => e.SessionNumber)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Select(e => new { Entry = e, e.AuthorPsychologist!.DisplayName })
            .ToListAsync(ct);

        return entries
            .Select(x => ToDto(x.Entry, x.DisplayName, callerPsychologistId.Value))
            .ToList();
    }

    public async Task<Result<LogbookEntryDto>> CreateAsync(Guid psychologistUserId, Guid patientUserId, SaveLogbookEntryRequest request, CancellationToken ct = default)
    {
        var callerPsychologistId = await AssignedPsychologistIdAsync(psychologistUserId, patientUserId, ct);
        if (callerPsychologistId is null)
        {
            return Result<LogbookEntryDto>.Failure(LogbookErrors.NotAssigned);
        }

        if (Validate(request) is { } error)
        {
            return Result<LogbookEntryDto>.Failure(error);
        }

        var entry = new LogbookEntry
        {
            PatientUserId = patientUserId,
            AuthorPsychologistId = callerPsychologistId.Value,
            SessionDate = request.SessionDate,
            SessionNumber = request.SessionNumber,
            CaseSummary = request.CaseSummary.Trim(),
            SessionActivities = request.SessionActivities.Trim(),
            Homework = Clean(request.Homework),
            NextSessionPlan = Clean(request.NextSessionPlan),
            FollowUpNeeded = request.FollowUpNeeded,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.LogbookEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        var authorName = await db.Psychologists.AsNoTracking()
            .Where(p => p.Id == callerPsychologistId.Value)
            .Select(p => p.DisplayName)
            .SingleAsync(ct);
        return Result<LogbookEntryDto>.Success(ToDto(entry, authorName, callerPsychologistId.Value));
    }

    public async Task<Result<LogbookEntryDto>> UpdateAsync(Guid psychologistUserId, Guid patientUserId, Guid entryId, SaveLogbookEntryRequest request, CancellationToken ct = default)
    {
        var callerPsychologistId = await AssignedPsychologistIdAsync(psychologistUserId, patientUserId, ct);
        if (callerPsychologistId is null)
        {
            return Result<LogbookEntryDto>.Failure(LogbookErrors.NotAssigned);
        }

        var entry = await db.LogbookEntries
            .Include(e => e.AuthorPsychologist)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.PatientUserId == patientUserId, ct);
        if (entry is null)
        {
            return Result<LogbookEntryDto>.Failure(LogbookErrors.EntryNotFound);
        }

        if (entry.AuthorPsychologistId != callerPsychologistId.Value)
        {
            // A colleague can read this entry, so 403 (unlike the 404s above) leaks nothing.
            return Result<LogbookEntryDto>.Failure(LogbookErrors.NotAuthor);
        }

        if (Validate(request) is { } error)
        {
            return Result<LogbookEntryDto>.Failure(error);
        }

        entry.SessionDate = request.SessionDate;
        entry.SessionNumber = request.SessionNumber;
        entry.CaseSummary = request.CaseSummary.Trim();
        entry.SessionActivities = request.SessionActivities.Trim();
        entry.Homework = Clean(request.Homework);
        entry.NextSessionPlan = Clean(request.NextSessionPlan);
        entry.FollowUpNeeded = request.FollowUpNeeded;
        entry.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Result<LogbookEntryDto>.Success(ToDto(entry, entry.AuthorPsychologist!.DisplayName, callerPsychologistId.Value));
    }

    /// <summary>The assignment row IS the authorization; its psychologist id doubles as the author id on create.</summary>
    private async Task<Guid?> AssignedPsychologistIdAsync(Guid psychologistUserId, Guid patientUserId, CancellationToken ct)
    {
        var row = await db.PatientAssignments.AsNoTracking()
            .Where(a => a.PatientUserId == patientUserId && a.Psychologist!.UserId == psychologistUserId)
            .Select(a => new { a.PsychologistId })
            .FirstOrDefaultAsync(ct);
        return row?.PsychologistId;
    }

    private static Error? Validate(SaveLogbookEntryRequest request)
    {
        if (request.SessionDate == default)
        {
            return LogbookErrors.SessionDateRequired;
        }

        if (request.SessionNumber < 1)
        {
            return LogbookErrors.SessionNumberInvalid;
        }

        if (string.IsNullOrWhiteSpace(request.CaseSummary))
        {
            return LogbookErrors.SummaryRequired;
        }

        if (string.IsNullOrWhiteSpace(request.SessionActivities))
        {
            return LogbookErrors.ActivitiesRequired;
        }

        return null;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static LogbookEntryDto ToDto(LogbookEntry e, string authorDisplayName, Guid callerPsychologistId) => new(
        e.Id, e.SessionDate, e.SessionNumber, e.CaseSummary, e.SessionActivities,
        e.Homework, e.NextSessionPlan, e.FollowUpNeeded,
        e.AuthorPsychologistId, authorDisplayName, e.AuthorPsychologistId == callerPsychologistId,
        e.CreatedAtUtc, e.UpdatedAtUtc);
}
