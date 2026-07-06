using Ardayasa.Application.Common;

namespace Ardayasa.Application.Patients;

/// <summary>
/// Psychologist-side logbook access, always gated on a <c>PatientAssignment</c>
/// for the calling psychologist. Every assigned psychologist reads all entries
/// for the patient; only the author edits their own; nothing is ever deleted.
/// There is deliberately no admin or patient surface for the logbook.
/// </summary>
public interface ILogbookService
{
    /// <summary>
    /// All entries for one patient, newest session first. Null when the caller
    /// has no assignment (caller returns 404 — deliberately indistinguishable
    /// from a nonexistent patient).
    /// </summary>
    Task<IReadOnlyList<LogbookEntryDto>?> ListAsync(Guid psychologistUserId, Guid patientUserId, CancellationToken ct = default);

    Task<Result<LogbookEntryDto>> CreateAsync(Guid psychologistUserId, Guid patientUserId, SaveLogbookEntryRequest request, CancellationToken ct = default);

    /// <summary>Author-only edit; the entry must belong to the given patient.</summary>
    Task<Result<LogbookEntryDto>> UpdateAsync(Guid psychologistUserId, Guid patientUserId, Guid entryId, SaveLogbookEntryRequest request, CancellationToken ct = default);
}
