using Ardayasa.Application.Common;

namespace Ardayasa.Application.Patients;

/// <summary>
/// Admin-side assignment management and the psychologist-side patient views it
/// unlocks. Admin methods never surface intake answers — only account basics and
/// a completed-or-not flag.
/// </summary>
public interface IPatientAssignmentService
{
    Task<PagedResult<AdminPatientListItemDto>> ListPatientsAsync(string? search, int page, int pageSize, CancellationToken ct = default);

    Task<Result> AssignAsync(Guid patientUserId, Guid psychologistId, Guid actorUserId, CancellationToken ct = default);

    Task<Result> UnassignAsync(Guid patientUserId, Guid psychologistId, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Patients assigned to the psychologist owning <paramref name="psychologistUserId"/>.</summary>
    Task<IReadOnlyList<PsychologistPatientListItemDto>> ListForPsychologistAsync(Guid psychologistUserId, CancellationToken ct = default);

    /// <summary>
    /// Intake detail for one patient, gated on an assignment to the calling
    /// psychologist. Null when no such assignment exists (caller returns 404 —
    /// deliberately indistinguishable from a nonexistent patient).
    /// </summary>
    Task<PsychologistPatientDetailDto?> GetPatientDetailForPsychologistAsync(Guid psychologistUserId, Guid patientUserId, CancellationToken ct = default);
}
