using Ardayasa.Application.Common;
using Ardayasa.Domain.Entities;

namespace Ardayasa.Application.Scheduling;

public record Slot(DateTime StartUtc, DateTime EndUtc);

/// <summary>
/// Pure slot generation: weekly rules plus dated exceptions, minus existing
/// active bookings, stepped by service duration + buffer. Slots are never
/// persisted — the booking table's DB constraints are the race guard.
/// All rule/exception times are wall-clock WIB; output is UTC.
/// </summary>
public static class SlotGenerator
{
    public static List<Slot> Generate(
        DateOnly fromWibDate,
        DateOnly toWibDate,
        int durationMinutes,
        int bufferMinutes,
        IReadOnlyList<AvailabilityRule> rules,
        IReadOnlyList<AvailabilityException> exceptions,
        IReadOnlyList<Slot> existingBookings,
        DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(durationMinutes, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(bufferMinutes);

        var slots = new List<Slot>();
        for (var date = fromWibDate; date <= toWibDate; date = date.AddDays(1))
        {
            var dayExceptions = exceptions.Where(x => x.Date == date).ToList();
            if (dayExceptions.Any(x => x.Kind == AvailabilityExceptionKind.Block && x.StartTime is null))
            {
                continue; // whole day blocked
            }

            var windows = rules
                .Where(r => r.DayOfWeek == date.DayOfWeek)
                .Select(r => (Start: ToMinutes(r.StartTime), End: ToMinutes(r.EndTime)))
                .Concat(dayExceptions
                    .Where(x => x.Kind == AvailabilityExceptionKind.Extra && x.StartTime is not null && x.EndTime is not null)
                    .Select(x => (Start: ToMinutes(x.StartTime!.Value), End: ToMinutes(x.EndTime!.Value))))
                .Where(w => w.Start < w.End);

            var blocks = dayExceptions
                .Where(x => x.Kind == AvailabilityExceptionKind.Block && x.StartTime is not null && x.EndTime is not null)
                .Select(x => (Start: ToMinutes(x.StartTime!.Value), End: ToMinutes(x.EndTime!.Value)))
                .ToList();

            foreach (var window in windows.SelectMany(w => Subtract(w, blocks)))
            {
                for (var start = window.Start; start + durationMinutes <= window.End; start += durationMinutes + bufferMinutes)
                {
                    var startUtc = Wib.ToUtc(date, TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(start)));
                    var endUtc = startUtc.AddMinutes(durationMinutes);
                    if (startUtc < nowUtc)
                    {
                        continue;
                    }

                    if (existingBookings.Any(b => startUtc < b.EndUtc && b.StartUtc < endUtc))
                    {
                        continue;
                    }

                    slots.Add(new Slot(startUtc, endUtc));
                }
            }
        }

        // Overlapping rules or an Extra duplicating a rule may produce duplicates.
        return slots
            .DistinctBy(s => s.StartUtc)
            .OrderBy(s => s.StartUtc)
            .ToList();
    }

    private static int ToMinutes(TimeOnly time) => (int)time.ToTimeSpan().TotalMinutes;

    /// <summary>Interval subtraction: the parts of <paramref name="window"/> not covered by any block.</summary>
    private static IEnumerable<(int Start, int End)> Subtract((int Start, int End) window, List<(int Start, int End)> blocks)
    {
        var remaining = new List<(int Start, int End)> { window };
        foreach (var block in blocks)
        {
            remaining = remaining
                .SelectMany<(int Start, int End), (int Start, int End)>(part =>
                {
                    if (block.End <= part.Start || part.End <= block.Start)
                    {
                        return [part]; // no overlap
                    }

                    var pieces = new List<(int, int)>();
                    if (part.Start < block.Start)
                    {
                        pieces.Add((part.Start, block.Start));
                    }

                    if (block.End < part.End)
                    {
                        pieces.Add((block.End, part.End));
                    }

                    return pieces;
                })
                .ToList();
        }

        return remaining;
    }
}
