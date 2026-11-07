using System;
using System.Collections.Generic;
using System.Linq;
using ItsycalForWindows.Models;
using ItsycalForWindows.Services;
using Xunit;

namespace ItsycalForWindows.Tests;

/// <summary>
/// Tests for <see cref="EventRange"/>: half-open range intersection, clipping, per-day counting
/// and grouping, including midnight boundaries, multi-day events, and empty ranges.
/// </summary>
public sealed class EventRangeTests
{
    [Fact]
    public void StartOfDayLocal_ReturnsLocalMidnight()
    {
        var date = TestDates.Day(2026, 11, 10);
        var start = EventRange.StartOfDayLocal(date);

        var wallClock = date.ToDateTime(TimeOnly.MinValue);
        Assert.Equal(wallClock, start.DateTime);
        Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(wallClock), start.Offset);
        Assert.Equal(TimeSpan.Zero, start.LocalDateTime.TimeOfDay);
        Assert.Equal(date, DateOnly.FromDateTime(start.LocalDateTime));
    }

    [Fact]
    public void GetLocalDaySpan_SingleDayTimedEvent()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Standup",
            TestDates.LocalMidnight(day).AddHours(9),
            TestDates.LocalMidnight(day).AddHours(10));

        var span = EventRange.GetLocalDaySpan(calendarEvent) ?? throw new InvalidOperationException("Expected a day span.");

        Assert.Equal(day, span.FirstDay);
        Assert.Equal(day, span.LastDay);
    }

    [Fact]
    public void GetLocalDaySpan_AllDayMultiDayEvent()
    {
        var firstDay = TestDates.Day(2026, 11, 13);
        var calendarEvent = TestDates.AllDay("Trip", firstDay, dayCount: 3);

        var span = EventRange.GetLocalDaySpan(calendarEvent) ?? throw new InvalidOperationException("Expected a day span.");

        Assert.Equal(firstDay, span.FirstDay);
        Assert.Equal(firstDay.AddDays(2), span.LastDay);
    }

    [Fact]
    public void GetLocalDaySpan_EventEndingAtMidnight_EndsOnThePreviousDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Late",
            TestDates.LocalMidnight(day).AddHours(22),
            TestDates.LocalMidnight(day.AddDays(1)));

        var span = EventRange.GetLocalDaySpan(calendarEvent) ?? throw new InvalidOperationException("Expected a day span.");

        Assert.Equal(day, span.FirstDay);
        Assert.Equal(day, span.LastDay);
    }

    [Fact]
    public void GetLocalDaySpan_InvertedEvent_ClampsToTheStartDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Backwards",
            TestDates.LocalMidnight(day).AddHours(10),
            TestDates.LocalMidnight(day).AddHours(8));

        var span = EventRange.GetLocalDaySpan(calendarEvent) ?? throw new InvalidOperationException("Expected a day span.");

        Assert.Equal(day, span.FirstDay);
        Assert.Equal(day, span.LastDay);
    }

    [Fact]
    public void GetLocalDaySpan_ThrowsForNullEvent()
    {
        Assert.Throws<ArgumentNullException>(() => { _ = EventRange.GetLocalDaySpan(null!); });
    }

    [Fact]
    public void IntersectsRange_EventInsideRange_ReturnsTrue()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var rangeEnd = TestDates.Day(2026, 12, 1);
        var calendarEvent = TestDates.Timed(
            "Inside",
            TestDates.LocalMidnight(TestDates.Day(2026, 11, 10)).AddHours(9),
            TestDates.LocalMidnight(TestDates.Day(2026, 11, 10)).AddHours(10));

        Assert.True(EventRange.IntersectsRange(calendarEvent, rangeStart, rangeEnd));
    }

    [Fact]
    public void IntersectsRange_EventBeforeRange_ReturnsFalse()
    {
        var calendarEvent = TestDates.AllDay("Before", TestDates.Day(2026, 10, 1), 1);

        Assert.False(EventRange.IntersectsRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void IntersectsRange_EventAfterRange_ReturnsFalse()
    {
        var calendarEvent = TestDates.AllDay("After", TestDates.Day(2027, 2, 1), 1);

        Assert.False(EventRange.IntersectsRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void IntersectsRange_EventEndingAtRangeStartMidnight_IsExcluded()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var calendarEvent = TestDates.Timed(
            "Ends at start",
            TestDates.LocalMidnight(rangeStart).AddDays(-1).AddHours(22),
            TestDates.LocalMidnight(rangeStart));

        Assert.False(EventRange.IntersectsRange(calendarEvent, rangeStart, TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void IntersectsRange_EventStartingAtRangeEndMidnight_IsExcluded()
    {
        var rangeEnd = TestDates.Day(2026, 12, 1);
        var calendarEvent = TestDates.AllDay("Starts at end", rangeEnd, 1);

        Assert.False(EventRange.IntersectsRange(calendarEvent, TestDates.Day(2026, 11, 1), rangeEnd));
    }

    [Fact]
    public void IntersectsRange_MultiDayEventSpanningRangeStart_ReturnsTrue()
    {
        var calendarEvent = TestDates.AllDay("Spans start", TestDates.Day(2026, 10, 28), dayCount: 6);

        Assert.True(EventRange.IntersectsRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void IntersectsRange_EventCoveringTheWholeRange_ReturnsTrue()
    {
        var calendarEvent = TestDates.AllDay("Long trip", TestDates.Day(2026, 10, 20), dayCount: 60);

        Assert.True(EventRange.IntersectsRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 11, 30)));
    }

    [Fact]
    public void IntersectsRange_EventOnTheLastDayOfRange_ReturnsTrue()
    {
        var calendarEvent = TestDates.AllDay("Last day", TestDates.Day(2026, 11, 30), 1);

        Assert.True(EventRange.IntersectsRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void IntersectsRange_EmptyAndInvertedRanges_ReturnFalse()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.AllDay("Same day", day, 1);

        Assert.False(EventRange.IntersectsRange(calendarEvent, day, day));
        Assert.False(EventRange.IntersectsRange(calendarEvent, day.AddDays(1), day));
    }

    [Fact]
    public void ClipToRange_EventFullyInside_IsUnchanged()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var rangeEnd = TestDates.Day(2026, 12, 1);
        var calendarEvent = TestDates.Timed(
            "Inside",
            TestDates.LocalMidnight(TestDates.Day(2026, 11, 10)).AddHours(9),
            TestDates.LocalMidnight(TestDates.Day(2026, 11, 10)).AddHours(10));

        var clipped = EventRange.ClipToRange(calendarEvent, rangeStart, rangeEnd);

        Assert.NotNull(clipped);
        Assert.Equal(calendarEvent, clipped);
    }

    [Fact]
    public void ClipToRange_MultiDayEventSpanningRangeStart_ClipsTheStart()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var rangeEnd = TestDates.Day(2026, 12, 1);
        var calendarEvent = TestDates.AllDay("Spans start", TestDates.Day(2026, 10, 28), dayCount: 6);

        var clipped = EventRange.ClipToRange(calendarEvent, rangeStart, rangeEnd);

        Assert.NotNull(clipped);
        Assert.Equal(EventRange.StartOfDayLocal(rangeStart), clipped!.Start);
        Assert.Equal(calendarEvent.End, clipped.End);
        Assert.Equal(calendarEvent.Subject, clipped.Subject);
        Assert.True(clipped.IsAllDay);
        Assert.Equal(calendarEvent.Location, clipped.Location);
    }

    [Fact]
    public void ClipToRange_MultiDayEventSpanningRangeEnd_ClipsTheEnd()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var rangeEnd = TestDates.Day(2026, 11, 30);
        var calendarEvent = TestDates.AllDay("Spans end", TestDates.Day(2026, 11, 28), dayCount: 5);

        var clipped = EventRange.ClipToRange(calendarEvent, rangeStart, rangeEnd);

        Assert.NotNull(clipped);
        Assert.Equal(calendarEvent.Start, clipped!.Start);
        Assert.Equal(EventRange.StartOfDayLocal(rangeEnd), clipped.End);
    }

    [Fact]
    public void ClipToRange_EventCoveringTheWholeRange_ClipsBothEnds()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var rangeEnd = TestDates.Day(2026, 11, 30);
        var calendarEvent = TestDates.AllDay("Long trip", TestDates.Day(2026, 10, 20), dayCount: 60);

        var clipped = EventRange.ClipToRange(calendarEvent, rangeStart, rangeEnd);

        Assert.NotNull(clipped);
        Assert.Equal(EventRange.StartOfDayLocal(rangeStart), clipped!.Start);
        Assert.Equal(EventRange.StartOfDayLocal(rangeEnd), clipped.End);
    }

    [Fact]
    public void ClipToRange_EventOutsideRange_ReturnsNull()
    {
        var calendarEvent = TestDates.AllDay("Before", TestDates.Day(2026, 10, 1), 2);

        Assert.Null(EventRange.ClipToRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void ClipToRange_EmptyAndInvertedRanges_ReturnNull()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.AllDay("Same day", day, 1);

        Assert.Null(EventRange.ClipToRange(calendarEvent, day, day));
        Assert.Null(EventRange.ClipToRange(calendarEvent, day.AddDays(1), day));
    }

    [Fact]
    public void ClipToRange_ZeroLengthEventInside_IsKept()
    {
        var rangeStart = TestDates.Day(2026, 11, 1);
        var rangeEnd = TestDates.Day(2026, 12, 1);
        var calendarEvent = TestDates.Timed(
            "Point",
            TestDates.LocalMidnight(TestDates.Day(2026, 11, 10)).AddHours(12),
            TestDates.LocalMidnight(TestDates.Day(2026, 11, 10)).AddHours(12));

        var clipped = EventRange.ClipToRange(calendarEvent, rangeStart, rangeEnd);

        Assert.NotNull(clipped);
        Assert.Equal(calendarEvent, clipped);
    }

    [Fact]
    public void ClipToRange_ZeroLengthEventOutside_ReturnsNull()
    {
        var calendarEvent = TestDates.Timed(
            "Point",
            TestDates.LocalMidnight(TestDates.Day(2026, 10, 1)).AddHours(12),
            TestDates.LocalMidnight(TestDates.Day(2026, 10, 1)).AddHours(12));

        Assert.Null(EventRange.ClipToRange(calendarEvent, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1)));
    }

    [Fact]
    public void ClipToRange_ThrowsForNullEvent()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = EventRange.ClipToRange(null!, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));
        });
    }

    [Fact]
    public void CountPerDay_CountsEventsPerDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        CalendarEvent[] events =
        [
            TestDates.Timed("First", TestDates.LocalMidnight(day).AddHours(9), TestDates.LocalMidnight(day).AddHours(10)),
            TestDates.Timed("Second", TestDates.LocalMidnight(day).AddHours(14), TestDates.LocalMidnight(day).AddHours(15)),
            TestDates.Timed("Other day", TestDates.LocalMidnight(day.AddDays(1)).AddHours(9), TestDates.LocalMidnight(day.AddDays(1)).AddHours(10)),
        ];

        var counts = EventRange.CountPerDay(events, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(2, counts[day]);
        Assert.Equal(1, counts[day.AddDays(1)]);
        Assert.Equal(2, counts.Count);
    }

    [Fact]
    public void CountPerDay_MultiDayEventAppearsOnEveryCoveredDay()
    {
        var firstDay = TestDates.Day(2026, 11, 13);
        var calendarEvent = TestDates.AllDay("Trip", firstDay, dayCount: 3);

        var counts = EventRange.CountPerDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(3, counts.Count);
        Assert.Equal(1, counts[firstDay]);
        Assert.Equal(1, counts[firstDay.AddDays(1)]);
        Assert.Equal(1, counts[firstDay.AddDays(2)]);
    }

    [Fact]
    public void CountPerDay_ClipsEventToTheRangeEdges()
    {
        var calendarEvent = TestDates.AllDay("Long", TestDates.Day(2026, 10, 25), dayCount: 40);
        var rangeStart = TestDates.Day(2026, 11, 27);
        var rangeEnd = TestDates.Day(2026, 12, 1);

        var counts = EventRange.CountPerDay([calendarEvent], rangeStart, rangeEnd);

        Assert.Equal(4, counts.Count);
        Assert.True(counts.Keys.All(day => day >= rangeStart && day < rangeEnd));
        Assert.Equal(1, counts[rangeStart]);
        Assert.Equal(1, counts[rangeEnd.AddDays(-1)]);
    }

    [Fact]
    public void CountPerDay_EventEndingAtMidnight_IsNotCountedOnTheNextDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Late",
            TestDates.LocalMidnight(day).AddHours(22),
            TestDates.LocalMidnight(day.AddDays(1)));

        var counts = EventRange.CountPerDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(1, counts.Count);
        Assert.Equal(1, counts[day]);
        Assert.False(counts.ContainsKey(day.AddDays(1)));
    }

    [Fact]
    public void CountPerDay_IgnoresEventsOutsideTheRange()
    {
        var calendarEvent = TestDates.AllDay("Outside", TestDates.Day(2026, 10, 1), dayCount: 3);

        var counts = EventRange.CountPerDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Empty(counts);
    }

    [Fact]
    public void CountPerDay_ZeroLengthEventIsCountedOnItsDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Point",
            TestDates.LocalMidnight(day).AddHours(12),
            TestDates.LocalMidnight(day).AddHours(12));

        var counts = EventRange.CountPerDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(1, counts.Count);
        Assert.Equal(1, counts[day]);
    }

    [Fact]
    public void CountPerDay_EmptyAndInvertedRanges_ReturnEmpty()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.AllDay("Same day", day, 1);

        Assert.Empty(EventRange.CountPerDay([calendarEvent], day, day));
        Assert.Empty(EventRange.CountPerDay([calendarEvent], day.AddDays(1), day));
    }

    [Fact]
    public void CountPerDay_ThrowsForNullEvents()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = EventRange.CountPerDay(null!, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));
        });
    }

    [Fact]
    public void GroupByDay_GroupsEventsByLocalDate()
    {
        var day = TestDates.Day(2026, 11, 10);
        CalendarEvent[] events =
        [
            TestDates.Timed("Morning", TestDates.LocalMidnight(day).AddHours(9), TestDates.LocalMidnight(day).AddHours(10)),
            TestDates.Timed("Afternoon", TestDates.LocalMidnight(day).AddHours(14), TestDates.LocalMidnight(day).AddHours(15)),
            TestDates.Timed("Other day", TestDates.LocalMidnight(day.AddDays(1)).AddHours(9), TestDates.LocalMidnight(day.AddDays(1)).AddHours(10)),
        ];

        var groups = EventRange.GroupByDay(events, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(2, groups.Count);
        Assert.Equal(2, groups[day].Count);
        Assert.Equal(1, groups[day.AddDays(1)].Count);
        Assert.Equal("Other day", groups[day.AddDays(1)][0].Subject);
    }

    [Fact]
    public void GroupByDay_OrdersEachDayByStartTime()
    {
        var day = TestDates.Day(2026, 11, 10);
        CalendarEvent[] events =
        [
            TestDates.Timed("Later", TestDates.LocalMidnight(day).AddHours(14), TestDates.LocalMidnight(day).AddHours(15)),
            TestDates.Timed("Earlier", TestDates.LocalMidnight(day).AddHours(9), TestDates.LocalMidnight(day).AddHours(10)),
        ];

        var groups = EventRange.GroupByDay(events, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(new[] { "Earlier", "Later" }, groups[day].Select(calendarEvent => calendarEvent.Subject).ToArray());
    }

    [Fact]
    public void GroupByDay_MultiDayEventAppearsOnEachDay()
    {
        var firstDay = TestDates.Day(2026, 11, 13);
        var calendarEvent = TestDates.AllDay("Trip", firstDay, dayCount: 3);

        var groups = EventRange.GroupByDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(3, groups.Count);
        Assert.Equal(1, groups[firstDay].Count);
        Assert.Equal(1, groups[firstDay.AddDays(1)].Count);
        Assert.Equal(1, groups[firstDay.AddDays(2)].Count);
    }

    [Fact]
    public void GroupByDay_OmitsDatesWithoutEvents()
    {
        var calendarEvent = TestDates.AllDay("Single", TestDates.Day(2026, 11, 13), dayCount: 1);

        var groups = EventRange.GroupByDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Equal(1, groups.Count);
        Assert.False(groups.ContainsKey(TestDates.Day(2026, 11, 14)));
    }

    [Fact]
    public void GroupByDay_IgnoresEventsOutsideTheRange()
    {
        var calendarEvent = TestDates.AllDay("Outside", TestDates.Day(2027, 1, 1), dayCount: 2);

        var groups = EventRange.GroupByDay([calendarEvent], TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));

        Assert.Empty(groups);
    }

    [Fact]
    public void GroupByDay_EmptyAndInvertedRanges_ReturnEmpty()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.AllDay("Same day", day, 1);

        Assert.Empty(EventRange.GroupByDay([calendarEvent], day, day));
        Assert.Empty(EventRange.GroupByDay([calendarEvent], day.AddDays(1), day));
    }

    [Fact]
    public void GroupByDay_ThrowsForNullEvents()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = EventRange.GroupByDay(null!, TestDates.Day(2026, 11, 1), TestDates.Day(2026, 12, 1));
        });
    }

    [Fact]
    public void CountPerDay_FeedsCalendarMonthEventCounts()
    {
        var month = new CalendarMonth(2026, 11, DayOfWeek.Sunday);
        var day = TestDates.Day(2026, 11, 10);
        CalendarEvent[] events =
        [
            TestDates.Timed("First", TestDates.LocalMidnight(day).AddHours(9), TestDates.LocalMidnight(day).AddHours(10)),
            TestDates.AllDay("Second", day, dayCount: 1),
        ];

        var counts = EventRange.CountPerDay(events, month.VisibleStart, month.VisibleEndExclusive);
        var withCounts = month.WithEventCounts(counts);

        Assert.Equal(2, withCounts.Days.Single(calendarDay => calendarDay.Date == day).EventCount);
        Assert.Equal(0, withCounts.Days.Single(calendarDay => calendarDay.Date == day.AddDays(1)).EventCount);
    }
}

