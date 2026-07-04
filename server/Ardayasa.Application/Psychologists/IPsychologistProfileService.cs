using Ardayasa.Application.Common;

namespace Ardayasa.Application.Psychologists;

/// <summary>
/// Profile management: self-service for psychologists (own record only) and
/// admin-on-behalf for any record (SPEC §3).
/// </summary>
public interface IPsychologistProfileService
{
    Task<PsychologistProfileDto?> GetOwnAsync(Guid userId, CancellationToken ct = default);

    Task<PsychologistProfileDto?> GetByIdAsync(Guid psychologistId, CancellationToken ct = default);

    Task<Result<PsychologistProfileDto>> UpdateOwnAsync(Guid userId, UpdatePsychologistProfileRequest request, CancellationToken ct = default);

    Task<Result<PsychologistProfileDto>> UpdateAsync(Guid psychologistId, UpdatePsychologistProfileRequest request, Guid actorUserId, CancellationToken ct = default);

    /// <summary>Stores the photo via IFileStorage and updates the record. Self-service overload uses the caller's own record.</summary>
    Task<Result<PsychologistProfileDto>> SetOwnPhotoAsync(Guid userId, Stream content, string fileName, CancellationToken ct = default);

    Task<Result<PsychologistProfileDto>> SetPhotoAsync(Guid psychologistId, Stream content, string fileName, Guid actorUserId, CancellationToken ct = default);
}
