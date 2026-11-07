using System;
using ItsycalForWindows.Models;
using Xunit;

namespace ItsycalForWindows.Tests;

/// <summary>
/// Tests for <see cref="CalendarEvent"/>: the local day span used by the calendar grid, including
/// all-day events, midnight boundaries, zero-length and inverted spans, and daylight-saving days.
/// </summary>
public sealed class CalendarEventTests
{
    private const string NewYork = "America/New_York";

    [Fact]
    public void TimedEvent_WithinOneDay_IsNotMultiDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Standup",
            TestDates.LocalMidnight(day).AddHours(9),
            TestDates.LocalMidnight(day).AddHours(10));

        TestDates.AssertDaySpan(calendarEvent, day, day);
        Assert.False(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void TimedEvent_EndingExactlyAtMidnight_DoesNotCoverTheNextDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Late meeting",
            TestDates.LocalMidnight(day).AddHours(22),
            TestDates.LocalMidnight(day.AddDays(1)));

        TestDates.AssertDaySpan(calendarEvent, day, day);
        Assert.Equal(TestDates.LocalMidnight(day.AddDays(1)), calendarEvent.End);
    }

    [Fact]
    public void TimedEvent_CrossingMidnight_CoversBothDays()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Overnight",
            TestDates.LocalMidnight(day).AddHours(22),
            TestDates.LocalMidnight(day.AddDays(1)).AddMinutes(30));

        TestDates.AssertDaySpan(calendarEvent, day, day.AddDays(1));
        Assert.True(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void TimedEvent_MultiDay_CoversEveryIntermediateDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Offsite",
            TestDates.LocalMidnight(day).AddHours(22),
            TestDates.LocalMidnight(day.AddDays(2)).AddHours(2));

        TestDates.AssertDaySpan(calendarEvent, day, day.AddDays(2));
        Assert.True(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void AllDayEvent_SingleDay_CoversOnlyThatDay()
    {
        var day = TestDates.Day(2026, 11, 12);
        var calendarEvent = TestDates.AllDay("Holiday", day, dayCount: 1);

        Assert.True(calendarEvent.IsAllDay);
        TestDates.AssertDaySpan(calendarEvent, day, day);
        Assert.False(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void AllDayEvent_ThreeDays_CoversThreeDays()
    {
        var day = TestDates.Day(2026, 11, 13);
        var calendarEvent = TestDates.AllDay("Trip", day, dayCount: 3);

        TestDates.AssertDaySpan(calendarEvent, day, day.AddDays(2));
        Assert.True(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void AllDayEvent_AcrossSpringForward_HasTwentyThreeHourDuration()
    {
        // 2026-03-08 is the US spring-forward day: the local day lasts 23 hours, so an all-day
        // event from local midnight to local midnight lasts 23 hours.
        var start = TestDates.ZoneMidnight(TestDates.Day(2026, 3, 8), NewYork);
        var end = TestDates.ZoneMidnight(TestDates.Day(2026, 3, 9), NewYork);

        Assert.Equal(TimeSpan.FromHours(23), end - start);

        var calendarEvent = new CalendarEvent("DST spring forward", start, end, IsAllDay: true);
        TestDates.AssertDaySpan(calendarEvent);
    }

    [Fact]
    public void AllDayEvent_AcrossFallBack_HasTwentyFiveHourDuration()
    {
        // 2026-11-01 is the US fall-back day: the local day lasts 25 hours.
        var start = TestDates.ZoneMidnight(TestDates.Day(2026, 11, 1), NewYork);
        var end = TestDates.ZoneMidnight(TestDates.Day(2026, 11, 2), NewYork);

        Assert.Equal(TimeSpan.FromHours(25), end - start);

        var calendarEvent = new CalendarEvent("DST fall back", start, end, IsAllDay: true);
        TestDates.AssertDaySpan(calendarEvent);
    }

    [Fact]
    public void AllDayEvent_WithTwentyThreeHourDuration_KeepsItsLocalDaySpan()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = new CalendarEvent(
            "Short all-day",
            TestDates.LocalMidnight(day),
            TestDates.LocalMidnight(day).AddHours(23),
            IsAllDay: true);

        Assert.Equal(TimeSpan.FromHours(23), calendarEvent.End - calendarEvent.Start);
        TestDates.AssertDaySpan(calendarEvent);
    }

    [Fact]
    public void AllDayEvent_WithTwentyFiveHourDuration_KeepsItsLocalDaySpan()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = new CalendarEvent(
            "Long all-day",
            TestDates.LocalMidnight(day),
            TestDates.LocalMidnight(day).AddHours(25),
            IsAllDay: true);

        Assert.Equal(TimeSpan.FromHours(25), calendarEvent.End - calendarEvent.Start);
        TestDates.AssertDaySpan(calendarEvent);
    }

    [Fact]
    public void ZeroLengthEvent_Midday_CountsOnItsDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Point in time",
            TestDates.LocalMidnight(day).AddHours(12),
            TestDates.LocalMidnight(day).AddHours(12));

        TestDates.AssertDaySpan(calendarEvent, day, day);
        Assert.False(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void ZeroLengthEvent_AtMidnight_CountsOnThatDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed("Point at midnight", TestDates.LocalMidnight(day), TestDates.LocalMidnight(day));

        TestDates.AssertDaySpan(calendarEvent, day, day);
        Assert.False(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void InvertedEvent_ClampsToOneDay()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Backwards",
            TestDates.LocalMidnight(day).AddHours(10),
            TestDates.LocalMidnight(day).AddHours(8));

        TestDates.AssertDaySpan(calendarEvent, day, day);
        Assert.False(calendarEvent.IsMultiDay);
    }

    [Fact]
    public void StartDate_UsesTheLocalCalendarDate()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(
            "Early",
            TestDates.LocalMidnight(day).AddMinutes(30),
            TestDates.LocalMidnight(day).AddHours(1));

        Assert.Equal(day, calendarEvent.StartDate);
        Assert.Equal(day, calendarEvent.EndDateInclusive);
    }

    [Fact]
    public void MultiDayTimedEvent_AcrossDstNight_MatchesLocalDayOverlap()
    {
        // 22:00 the evening before the US fall-back night to 03:00 the next morning.
        var start = TestDates.ZoneTime(TestDates.Day(2026, 11, 1), 22, NewYork);
        var end = TestDates.ZoneTime(TestDates.Day(2026, 11, 2), 3, NewYork);

        Assert.Equal(TimeSpan.FromHours(5), end - start);

        var calendarEvent = TestDates.Timed("Overnight DST", start, end);
        TestDates.AssertDaySpan(calendarEvent);
    }

    [Fact]
    public void DaySpan_MatchesLocalDayOverlap_ForRepresentativeEvents()
    {
        var day = TestDates.Day(2026, 11, 10);
        CalendarEvent[] events =
        [
            TestDates.Timed("within day", TestDates.LocalMidnight(day).AddHours(9), TestDates.LocalMidnight(day).AddHours(10)),
            TestDates.Timed("ends at midnight", TestDates.LocalMidnight(day).AddHours(20), TestDates.LocalMidnight(day.AddDays(1))),
            TestDates.Timed("crosses midnight", TestDates.LocalMidnight(day).AddHours(22), TestDates.LocalMidnight(day.AddDays(1)).AddMinutes(30)),
            TestDates.Timed("multi day", TestDates.LocalMidnight(day).AddHours(22), TestDates.LocalMidnight(day.AddDays(3)).AddHours(1)),
            TestDates.AllDay("all day single", day, 1),
            TestDates.AllDay("all day multi", day, 4),
            TestDates.Timed("zero length", TestDates.LocalMidnight(day).AddHours(12), TestDates.LocalMidnight(day).AddHours(12)),
            new CalendarEvent("spring forward", TestDates.ZoneMidnight(TestDates.Day(2026, 3, 8), NewYork), TestDates.ZoneMidnight(TestDates.Day(2026, 3, 9), NewYork), IsAllDay: true),
            new CalendarEvent("fall back", TestDates.ZoneMidnight(TestDates.Day(2026, 11, 1), NewYork), TestDates.ZoneMidnight(TestDates.Day(2026, 11, 2), NewYork), IsAllDay: true),
        ];

        foreach (var calendarEvent in events)
        {
            TestDates.AssertDaySpan(calendarEvent);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void DisplayTitle_FallsBackForBlankSubjects(string subject)
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed(subject, TestDates.LocalMidnight(day), TestDates.LocalMidnight(day).AddHours(1));

        Assert.Equal("(no title)", calendarEvent.DisplayTitle);
    }

    [Fact]
    public void DisplayTitle_ReturnsTheSubjectWhenPresent()
    {
        var day = TestDates.Day(2026, 11, 10);
        var calendarEvent = TestDates.Timed("Planning", TestDates.LocalMidnight(day), TestDates.LocalMidnight(day).AddHours(1));

        Assert.Equal("Planning", calendarEvent.DisplayTitle);
    }
}
