namespace Ardayasa.Domain.Entities;

public enum BookingMode
{
    Offline,
    Online,
}

/// <summary>Booking lifecycle per SPEC §6.3. Transitions are guarded by <see cref="BookingStateMachine"/>.</summary>
public enum BookingStatus
{
    PendingPayment,
    AwaitingVerification,
    Confirmed,
    Completed,
    NoShow,
    Cancelled,
    Expired,
}

/// <summary>
/// The explicit, guarded booking state machine (SPEC §6.3):
/// PendingPayment → AwaitingVerification → Confirmed → Completed, with side
/// branches Expired (timeout from PendingPayment only), Cancelled (per policy),
/// and NoShow (from Confirmed, after the session time).
/// </summary>
public static class BookingStateMachine
{
    /// <summary>Statuses that hold the slot — they block other bookings from taking it.</summary>
    public static readonly BookingStatus[] ActiveStatuses =
    [
        BookingStatus.PendingPayment,
        BookingStatus.AwaitingVerification,
        BookingStatus.Confirmed,
    ];

    private static readonly Dictionary<BookingStatus, BookingStatus[]> Transitions = new()
    {
        [BookingStatus.PendingPayment] = [BookingStatus.AwaitingVerification, BookingStatus.Expired, BookingStatus.Cancelled],
        [BookingStatus.AwaitingVerification] = [BookingStatus.Confirmed, BookingStatus.Cancelled],
        [BookingStatus.Confirmed] = [BookingStatus.Completed, BookingStatus.NoShow, BookingStatus.Cancelled],
        [BookingStatus.Completed] = [],
        [BookingStatus.NoShow] = [],
        [BookingStatus.Cancelled] = [],
        [BookingStatus.Expired] = [],
    };

    public static bool CanTransition(BookingStatus from, BookingStatus to)
        => Transitions[from].Contains(to);
}

/// <summary>
/// A patient's booking of one session slot. Times are UTC; the slot is defined
/// by (PsychologistId, StartUtc, EndUtc). Price and duration are snapshots taken
/// at booking time so later catalog edits never rewrite history. Double-booking
/// is prevented at the DB level: a partial unique index on
/// (PsychologistId, StartUtc) over active statuses, plus (Postgres only) an
/// exclusion constraint on the overlapping time range.
/// </summary>
public class Booking
{
    public Guid Id { get; set; }

    public Guid PatientUserId { get; set; }

    public Guid PsychologistId { get; set; }

    public Psychologist? Psychologist { get; set; }

    public Guid ServiceId { get; set; }

    public Service? Service { get; set; }

    public BookingMode Mode { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    /// <summary>Snapshot of the service duration at booking time.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Snapshot of the mode price (IDR) at booking time.</summary>
    public decimal PriceIdr { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;

    /// <summary>
    /// Meeting link for online sessions, set by the psychologist (own bookings)
    /// or admin. Shown to the patient only once the booking is Confirmed.
    /// </summary>
    public string? ZoomLink { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>End of the 30-minute payment window while PendingPayment (Hangfire expiry lands in Phase 3).</summary>
    public DateTime? PaymentDueAtUtc { get; set; }

    public DateTime? StatusChangedAtUtc { get; set; }

    /// <summary>Guarded transition; returns false (and changes nothing) when the move is invalid.</summary>
    public bool TryTransition(BookingStatus to, DateTime nowUtc)
    {
        if (!BookingStateMachine.CanTransition(Status, to))
        {
            return false;
        }

        Status = to;
        StatusChangedAtUtc = nowUtc;
        return true;
    }
}
