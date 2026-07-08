using Ardayasa.Application.Common;
using Ardayasa.Application.Common.Interfaces;
using Ardayasa.Application.Scheduling;
using Ardayasa.Domain.Entities;
using Ardayasa.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ardayasa.Infrastructure.Scheduling;

public class ClinicSettingsService(AppDbContext db, IAuditLogger audit) : IClinicSettingsService
{
    public const string SlotBufferMinutesKey = "SlotBufferMinutes";

    /// <summary>Default confirmed by Ihsan 2026-07-07: slots run back-to-back.</summary>
    public const int DefaultSlotBufferMinutes = 0;

    public async Task<ClinicSettingsDto> GetAsync(CancellationToken ct = default)
        => new(await GetSlotBufferMinutesAsync(ct));

    public async Task<int> GetSlotBufferMinutesAsync(CancellationToken ct = default)
    {
        var value = await db.ClinicSettings.AsNoTracking()
            .Where(s => s.Key == SlotBufferMinutesKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);
        return value is not null && int.TryParse(value, out var minutes) ? minutes : DefaultSlotBufferMinutes;
    }

    public async Task<Result<ClinicSettingsDto>> UpdateAsync(
        ClinicSettingsDto settings, Guid actorUserId, CancellationToken ct = default)
    {
        if (settings.SlotBufferMinutes is < 0 or > 120)
        {
            return Result<ClinicSettingsDto>.Failure(SchedulingErrors.InvalidBuffer);
        }

        var row = await db.ClinicSettings.FirstOrDefaultAsync(s => s.Key == SlotBufferMinutesKey, ct);
        if (row is null)
        {
            db.ClinicSettings.Add(new ClinicSetting
            {
                Key = SlotBufferMinutesKey,
                Value = settings.SlotBufferMinutes.ToString(),
            });
        }
        else
        {
            row.Value = settings.SlotBufferMinutes.ToString();
        }

        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorUserId, "settings.updated", "ClinicSetting", SlotBufferMinutesKey,
            new { settings.SlotBufferMinutes }, ct);
        return Result<ClinicSettingsDto>.Success(settings);
    }
}
