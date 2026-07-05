using Ardayasa.Application.Common;

namespace Ardayasa.Application.Psychologists;

/// <summary>
/// Profile management: psychologists read their own record; all edits are
/// admin-only (SPEC §3, clinic decision 2026-07-05).
/// </summary>
public interface IPsychologistProfileService
{
    Task<PsychologistProfileDto?> GetOwnAsync(Guid userId, CancellationToken ct = default);

    Task<PsychologistProfileDto?> GetByIdAsync(Guid psychologistId, CancellationToken ct = default);

    Task<Result<PsychologistProfileDto>> UpdateAsync(Guid psychologistId, UpdatePsychologistProfileRequest request, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Stores the photo via IFileStorage and updates the record.</summary>
    Task<Result<PsychologistProfileDto>> SetPhotoAsync(Guid psychologistId, Stream content, string fileName, Guid actorUserId, CancellationToken ct = default);
}
