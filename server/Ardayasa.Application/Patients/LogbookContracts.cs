namespace Ardayasa.Application.Patients;

/// <summary>
/// Create/update payload for a logbook entry. Session date, session number,
/// case summary, and session activities are required; homework and the next
/// session plan may be left empty.
/// </summary>
public record SaveLogbookEntryRequest(
    DateOnly SessionDate,
    int SessionNumber,
    string CaseSummary,
    string SessionActivities,
    string? Homework,
    string? NextSessionPlan,
    bool FollowUpNeeded);

/// <summary>Logbook entry as shown to an assigned psychologist. <see cref="IsOwn"/> tells the client whether to offer editing.</summary>
public record LogbookEntryDto(
    Guid Id,
    DateOnly SessionDate,
    int SessionNumber,
    string CaseSummary,
    string SessionActivities,
    string? Homework,
    string? NextSessionPlan,
    bool FollowUpNeeded,
    Guid AuthorPsychologistId,
    string AuthorDisplayName,
    bool IsOwn,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
