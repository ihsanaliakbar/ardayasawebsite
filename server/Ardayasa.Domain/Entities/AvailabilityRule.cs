namespace Ardayasa.Domain.Entities;

/// <summary>
/// One recurring weekly availability window for a psychologist, e.g. "Monday
/// 09:00–13:00". Times are wall-clock WIB (Asia/Jakarta) by definition — weekly
/// recurrence is inherently local time; conversion to UTC happens at slot
/// generation. Admin-managed only (clinic decision 2026-07-07); psychologists
/// see their own rules read-only.
/// </summary>
public class AvailabilityRule
{
    public Guid Id { get; set; }

    public Guid PsychologistId { get; set; }

    public Psychologist? Psychologist { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    /// <summary>Window start, wall-clock WIB.</summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>Window end (exclusive), wall-clock WIB.</summary>
    public TimeOnly EndTime { get; set; }
}
