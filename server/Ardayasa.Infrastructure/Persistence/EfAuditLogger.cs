using System.Text.Json;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Domain.Entities;

namespace Ardayasa.Infrastructure.Persistence;

public class EfAuditLogger(AppDbContext db) : IAuditLogger
{
    public async Task LogAsync(
        Guid? actorUserId,
        string action,
        string entityType,
        string? entityId = null,
        object? data = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DataJson = data is null ? null : JsonSerializer.Serialize(data),
            TimestampUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
