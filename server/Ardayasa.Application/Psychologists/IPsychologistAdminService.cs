using Ardayasa.Application.Common;

namespace Ardayasa.Application.Psychologists;

public record InvitePsychologistRequest(string FullName, string Email, string? Title);

public record PsychologistDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? Title,
    string Email,
    bool IsActive,
    bool InvitationAccepted);

/// <summary>Admin-only management of psychologist accounts (SPEC §3: admin-invited, never self-registered).</summary>
public interface IPsychologistAdminService
{
    /// <summary>Creates the user + psychologist record and emails a one-time invitation link.</summary>
    Task<Result<PsychologistDto>> InviteAsync(InvitePsychologistRequest request, Guid actorUserId, CancellationToken ct = default);

    Task<Result> ResendInvitationAsync(Guid psychologistId, Guid actorUserId, CancellationToken ct = default);

    Task<IReadOnlyList<PsychologistDto>> ListAsync(CancellationToken ct = default);
}
