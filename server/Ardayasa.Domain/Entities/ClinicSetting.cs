namespace Ardayasa.Domain.Entities;

/// <summary>
/// Key–value store for admin-configurable clinic settings (slot buffer now;
/// cancellation/reschedule window in Phase 4). Updates are audit-logged.
/// </summary>
public class ClinicSetting
{
    public required string Key { get; set; }

    public required string Value { get; set; }
}
