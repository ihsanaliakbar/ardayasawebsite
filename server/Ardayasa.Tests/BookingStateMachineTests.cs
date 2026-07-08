using Ardayasa.Domain.Entities;

namespace Ardayasa.Tests;

/// <summary>
/// The guarded booking state machine (SPEC §6.3): every allowed transition,
/// and — by exhaustion — the impossibility of every other one.
/// </summary>
public class BookingStateMachineTests
{
    private static readonly (BookingStatus From, BookingStatus To)[] Allowed =
    [
        (BookingStatus.PendingPayment, BookingStatus.AwaitingVerification),
        (BookingStatus.PendingPayment, BookingStatus.Expired),
        (BookingStatus.PendingPayment, BookingStatus.Cancelled),
        (BookingStatus.AwaitingVerification, BookingStatus.Confirmed),
        (BookingStatus.AwaitingVerification, BookingStatus.Cancelled),
        (BookingStatus.Confirmed, BookingStatus.Completed),
        (BookingStatus.Confirmed, BookingStatus.NoShow),
        (BookingStatus.Confirmed, BookingStatus.Cancelled),
    ];

    [Fact]
    public void ExactlyTheSpecifiedTransitionsAreAllowed()
    {
        foreach (var from in Enum.GetValues<BookingStatus>())
        {
            foreach (var to in Enum.GetValues<BookingStatus>())
            {
                Assert.Equal(
                    Allowed.Contains((from, to)),
                    BookingStateMachine.CanTransition(from, to));
            }
        }
    }

    [Fact]
    public void TryTransitionGuardsAndStamps()
    {
        var nowUtc = DateTime.UtcNow;
        var booking = new Booking { Status = BookingStatus.PendingPayment };

        // Expired → Confirmed must be impossible; a failed attempt changes nothing.
        Assert.False(booking.TryTransition(BookingStatus.Confirmed, nowUtc));
        Assert.Equal(BookingStatus.PendingPayment, booking.Status);
        Assert.Null(booking.StatusChangedAtUtc);

        Assert.True(booking.TryTransition(BookingStatus.AwaitingVerification, nowUtc));
        Assert.Equal(BookingStatus.AwaitingVerification, booking.Status);
        Assert.Equal(nowUtc, booking.StatusChangedAtUtc);

        Assert.True(booking.TryTransition(BookingStatus.Confirmed, nowUtc));
        Assert.False(booking.TryTransition(BookingStatus.PendingPayment, nowUtc));
    }

    [Fact]
    public void ExpiryIsOnlyReachableFromPendingPayment()
    {
        foreach (var from in Enum.GetValues<BookingStatus>().Where(s => s != BookingStatus.PendingPayment))
        {
            Assert.False(BookingStateMachine.CanTransition(from, BookingStatus.Expired));
        }
    }
}
