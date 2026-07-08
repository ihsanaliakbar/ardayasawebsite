using Ardayasa.Application.Scheduling;
using Ardayasa.Domain.Entities;

namespace Ardayasa.Tests;

/// <summary>
/// Pure slot-generation logic: WIB→UTC conversion (via TimeZoneInfo, never a
/// hardcoded +7), duration+buffer stepping, exceptions, and overlap exclusion.
/// </summary>
public class SlotGeneratorTests
{
    // 2026-07-13 is a Monday; all tests generate for this single WIB date.
    private static readonly DateOnly Monday = new(2026, 7, 13);

    // "Now" far before the generated range so no slot is filtered as past.
    private static readonly DateTime NowUtc = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    private static AvailabilityRule Rule(DayOfWeek day, string start, string end) => new()
    {
        Id = Guid.NewGuid(),
        PsychologistId = Guid.Empty,
        DayOfWeek = day,
        StartTime = TimeOnly.Parse(start),
        EndTime = TimeOnly.Parse(end),
    };

    private static AvailabilityException Exception(
        DateOnly date, AvailabilityExceptionKind kind, string? start = null, string? end = null) => new()
    {
        Id = Guid.NewGuid(),
        PsychologistId = Guid.Empty,
        Date = date,
        Kind = kind,
        StartTime = start is null ? null : TimeOnly.Parse(start),
        EndTime = end is null ? null : TimeOnly.Parse(end),
    };

    [Fact]
    public void ConvertsWibWallClockToUtc()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, durationMinutes: 60, bufferMinutes: 0,
            [Rule(DayOfWeek.Monday, "09:00", "11:00")], [], [], NowUtc);

        // 09:00 and 10:00 WIB are 02:00 and 03:00 UTC.
        Assert.Equal(
            [new DateTime(2026, 7, 13, 2, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 13, 3, 0, 0, DateTimeKind.Utc)],
            slots.Select(s => s.StartUtc));
        Assert.All(slots, s => Assert.Equal(60, (s.EndUtc - s.StartUtc).TotalMinutes));
    }

    [Fact]
    public void BufferSpacesSlotsAndCutsThoseThatNoLongerFit()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, durationMinutes: 60, bufferMinutes: 15,
            [Rule(DayOfWeek.Monday, "09:00", "12:00")], [], [], NowUtc);

        // 09:00 fits, 10:15 fits (ends 11:15); 11:30 would end 12:30 > 12:00.
        Assert.Equal(["09:00", "10:15"], slots.Select(s => TimeOnly.FromDateTime(Application.Common.Wib.ToWib(s.StartUtc)).ToString("HH:mm")));
    }

    [Fact]
    public void RulesOnOtherDaysProduceNothing()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [Rule(DayOfWeek.Tuesday, "09:00", "17:00")], [], [], NowUtc);
        Assert.Empty(slots);
    }

    [Fact]
    public void FullDayBlockRemovesEverythingIncludingExtras()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [Rule(DayOfWeek.Monday, "09:00", "12:00")],
            [
                Exception(Monday, AvailabilityExceptionKind.Extra, "13:00", "15:00"),
                Exception(Monday, AvailabilityExceptionKind.Block),
            ],
            [], NowUtc);
        Assert.Empty(slots);
    }

    [Fact]
    public void PartialBlockSplitsTheWindow()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [Rule(DayOfWeek.Monday, "09:00", "13:00")],
            [Exception(Monday, AvailabilityExceptionKind.Block, "10:00", "11:00")],
            [], NowUtc);

        // 09:00 fits before the block; 11:00 and 12:00 fit after; 10:00 is gone.
        Assert.Equal(["09:00", "11:00", "12:00"],
            slots.Select(s => TimeOnly.FromDateTime(Application.Common.Wib.ToWib(s.StartUtc)).ToString("HH:mm")));
    }

    [Fact]
    public void ExtraExceptionAddsAOneOffWindow()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [],
            [Exception(Monday, AvailabilityExceptionKind.Extra, "18:00", "20:00")],
            [], NowUtc);
        Assert.Equal(2, slots.Count);
    }

    [Fact]
    public void ExistingActiveBookingExcludesOverlappingSlots()
    {
        // Booking 09:30–10:30 WIB (02:30–03:30 UTC) knocks out the 09:00 and 10:00 slots.
        var booking = new Slot(
            new DateTime(2026, 7, 13, 2, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 13, 3, 30, 0, DateTimeKind.Utc));

        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [Rule(DayOfWeek.Monday, "09:00", "13:00")], [], [booking], NowUtc);

        Assert.Equal(["11:00", "12:00"],
            slots.Select(s => TimeOnly.FromDateTime(Application.Common.Wib.ToWib(s.StartUtc)).ToString("HH:mm")));
    }

    [Fact]
    public void PastSlotsAreExcluded()
    {
        // Now = 09:30 WIB on the generated day: the 09:00 slot is gone, 10:00 remains.
        var nowUtc = new DateTime(2026, 7, 13, 2, 30, 0, DateTimeKind.Utc);
        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [Rule(DayOfWeek.Monday, "09:00", "11:00")], [], [], nowUtc);

        var slot = Assert.Single(slots);
        Assert.Equal(new DateTime(2026, 7, 13, 3, 0, 0, DateTimeKind.Utc), slot.StartUtc);
    }

    [Fact]
    public void OverlappingWindowsDoNotDuplicateSlots()
    {
        var slots = SlotGenerator.Generate(
            Monday, Monday, 60, 0,
            [Rule(DayOfWeek.Monday, "09:00", "11:00"), Rule(DayOfWeek.Monday, "09:00", "11:00")],
            [Exception(Monday, AvailabilityExceptionKind.Extra, "09:00", "11:00")],
            [], NowUtc);
        Assert.Equal(2, slots.Count);
    }
}
