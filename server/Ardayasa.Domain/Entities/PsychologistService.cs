namespace Ardayasa.Domain.Entities;

/// <summary>
/// Admin-managed mapping of which services a psychologist can be booked for
/// (clinic decision 2026-07-07). The booking wizard only offers valid pairings;
/// a unique DB index prevents duplicates.
/// </summary>
public class PsychologistService
{
    public Guid Id { get; set; }

    public Guid PsychologistId { get; set; }

    public Psychologist? Psychologist { get; set; }

    public Guid ServiceId { get; set; }

    public Service? Service { get; set; }
}
