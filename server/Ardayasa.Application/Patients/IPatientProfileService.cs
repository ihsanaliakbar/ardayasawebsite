using Ardayasa.Application.Common;

namespace Ardayasa.Application.Patients;

/// <summary>Self-service intake profile for the logged-in patient.</summary>
public interface IPatientProfileService
{
    /// <summary>Null when the patient hasn't saved the form yet.</summary>
    Task<PatientProfileDto?> GetOwnAsync(Guid userId, CancellationToken ct = default);

    Task<Result<PatientProfileDto>> UpsertOwnAsync(Guid userId, UpdatePatientProfileRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<AssignedPsychologistDto>> GetAssignedPsychologistsAsync(Guid userId, CancellationToken ct = default);
}
