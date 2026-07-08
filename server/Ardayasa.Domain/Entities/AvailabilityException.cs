namespace Ardayasa.Domain.Entities;

public enum AvailabilityExceptionKind
{
    /// <summary>Removes availability on the date (whole day when no times are set).</summary>
    Block,

    /// <summary>Adds a one-off availability window on the date (times required).</summary>
    Extra,
}

/// <summary>
/// A dated override of the weekly availability rules. Date and times are
/// wall-clock WIB, same convention as <see cref="AvailabilityRule"/>.
/// </summary>
public class AvailabilityException
{
    public Guid Id { get; set; }

    public Guid PsychologistId { get; set; }

    public Psychologist? Psychologist { get; set; }

    /// <summary>The WIB calendar date the exception applies to.</summary>
    public DateOnly Date { get; set; }

    public AvailabilityExceptionKind Kind { get; set; }

    /// <summary>Null together with <see cref="EndTime"/> means the whole day (Block only).</summary>
    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }
}
