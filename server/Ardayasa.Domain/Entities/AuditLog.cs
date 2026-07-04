namespace Ardayasa.Domain.Entities;

/// <summary>
/// Append-only log of admin/system actions on sensitive resources
/// (bookings, payments, settings, accounts). Written via IAuditLogger.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Null for system-initiated actions (e.g. Hangfire jobs).</summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>Machine-readable action name, e.g. "psychologist.invited".</summary>
    public required string Action { get; set; }

    public required string EntityType { get; set; }

    public string? EntityId { get; set; }

    /// <summary>JSON payload with action-specific details. Never contains secrets or PII beyond what the action requires.</summary>
    public string? DataJson { get; set; }

    public DateTime TimestampUtc { get; set; }
}
