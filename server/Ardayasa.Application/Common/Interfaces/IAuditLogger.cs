namespace Ardayasa.Application.Common.Interfaces;

/// <summary>Writes append-only audit entries for admin/system actions.</summary>
public interface IAuditLogger
{
    Task LogAsync(
        Guid? actorUserId,
        string action,
        string entityType,
        string? entityId = null,
        object? data = null,
        CancellationToken ct = default);
}
