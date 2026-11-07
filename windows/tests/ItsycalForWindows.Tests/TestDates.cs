using System;
using System.Collections.Generic;
using ItsycalForWindows.Models;
using ItsycalForWindows.Services;
using Xunit;

namespace ItsycalForWindows.Tests;

/// <summary>
/// Date helpers shared by the calendar tests.
/// </summary>
/// <remarks>
/// Every instant is built from <see cref="EventRange.StartOfDayLocal"/> (or from an explicit IANA
/// time zone for the daylight-saving cases), never from a hard-coded UTC offset. That keeps the
/// assertions correct in whatever local time zone the test runner happens to use.
/// </remarks>
internal static class TestDates
{
    /// <summary>Creates a calendar date.</summary>
    public static DateOnly Day(int year, int month, int day) => new(year, month, day);

    /// <summary>Local midnight at the start of <paramref name="date"/>.</summary>
    public static DateTimeOffset LocalMidnight(DateOnly date) => EventRange.StartOfDayLocal(date);

    /// <summary>A timed event with the given local instants.</summary>
    public static CalendarEvent Timed(string subject, DateTimeOffset start, DateTimeOffset end) =>
        new(subject, start, end);

    /// <summary>
    /// An all-day event starting at local midnight of <paramref name="firstDay"/> and lasting
    /// <paramref name="dayCount"/> whole days (the end is exclusive and sits at local midnight of
    /// the day after the last covered day, like the appointment store reports all-day events).
    /// </summary>
    public static CalendarEvent AllDay(string subject, DateOnly firstDay, int dayCount) =>
        new(subject, LocalMidnight(firstDay), LocalMidnight(firstDay.AddDays(dayCount)), IsAllDay: true);

    /// <summary>
    /// A local wall-clock time in an explicit IANA time zone, for events that straddle that
    /// zone's daylight-saving transitions independently of the runner's local zone.
    /// </summary>
    public static DateTimeOffset ZoneTime(DateOnly date, int hour, string ianaTimeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
        var wallTime = date.ToDateTime(TimeOnly.MinValue).AddHours(hour);
        return new DateTimeOffset(wallTime, zone.GetUtcOffset(wallTime));
    }

    /// <summary>
    /// Midnight of <paramref name="date"/> in an explicit IANA time zone, for events that straddle
    /// that zone's daylight-saving transitions independently of the runner's local zone.
    /// </summary>
    public static DateTimeOffset ZoneMidnight(DateOnly date, string ianaTimeZoneId)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
        var midnight = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, zone.GetUtcOffset(midnight));
    }

    /// <summary>
    /// Brute-force specification oracle: the local calendar days whose half-open
    /// <c>[local midnight, next local midnight)</c> interval overlaps the event's instant
    /// interval. A zero-length event counts as one day (the day of its start).
    /// </summary>
    public static IReadOnlyList<DateOnly> CoveredLocalDays(CalendarEvent calendarEvent)
    {
        if (calendarEvent.End <= calendarEvent.Start)
        {
            return [calendarEvent.StartDate];
        }

        var first = calendarEvent.StartDate.AddDays(-1);
        var last = DateOnly.FromDateTime(calendarEvent.End.LocalDateTime).AddDays(1);
        var covered = new List<DateOnly>();

        for (var day = first; day <= last; day = day.AddDays(1))
        {
            var dayStart = LocalMidnight(day);
            var dayEnd = LocalMidnight(day.AddDays(1));
            if (calendarEvent.Start < dayEnd && calendarEvent.End > dayStart)
            {
                covered.Add(day);
            }
        }

        return covered;
    }

    /// <summary>
    /// Asserts the model's day span against the overlap oracle: first covered local day and last
    /// covered local day, plus the multi-day flag derived from them.
    /// </summary>
    public static void AssertDaySpan(CalendarEvent calendarEvent)
    {
        var covered = CoveredLocalDays(calendarEvent);

        Assert.NotEmpty(covered);
        Assert.Equal(covered[0], calendarEvent.StartDate);
        Assert.Equal(covered[^1], calendarEvent.EndDateInclusive);
        Assert.Equal(covered.Count > 1, calendarEvent.IsMultiDay);
    }

    /// <summary>Asserts the model's day span against explicit first/last local days.</summary>
    public static void AssertDaySpan(CalendarEvent calendarEvent, DateOnly expectedFirst, DateOnly expectedLast)
    {
        Assert.Equal(expectedFirst, calendarEvent.StartDate);
        Assert.Equal(expectedLast, calendarEvent.EndDateInclusive);
        Assert.Equal(expectedLast > expectedFirst, calendarEvent.IsMultiDay);
    }
}
