using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ItsycalForWindows.Models;
using Xunit;

namespace ItsycalForWindows.Tests;

/// <summary>
/// Tests for <see cref="CalendarMonth"/>: the 42-cell grid, week-start handling, ISO week
/// numbers, month navigation, and the event-count overlay.
/// </summary>
public sealed class CalendarMonthTests
{
    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void Constructor_ProducesFortyTwoConsecutiveCells(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);

        Assert.Equal(42, CalendarMonth.CellCount);
        Assert.Equal(42, month.Days.Count);

        for (var index = 0; index < CalendarMonth.CellCount; index++)
        {
            Assert.Equal(month.VisibleStart.AddDays(index), month.Days[index].Date);
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, 2026, 11, 1)]
    [InlineData(DayOfWeek.Monday, 2026, 10, 26)]
    public void November2026_VisibleStartHonorsWeekStart(DayOfWeek weekStart, int year, int month, int day)
    {
        var grid = new CalendarMonth(2026, 11, weekStart);

        Assert.Equal(weekStart, grid.WeekStart);
        Assert.Equal(TestDates.Day(year, month, day), grid.VisibleStart);
        Assert.Equal(weekStart, grid.Days[0].Date.DayOfWeek);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, 2026, 11, 1, 2026, 12, 13)]
    [InlineData(DayOfWeek.Monday, 2026, 10, 26, 2026, 12, 7)]
    public void November2026_VisibleRangeIsHalfOpen(
        DayOfWeek weekStart,
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay)
    {
        var grid = new CalendarMonth(2026, 11, weekStart);

        Assert.Equal(TestDates.Day(startYear, startMonth, startDay), grid.VisibleStart);
        Assert.Equal(TestDates.Day(endYear, endMonth, endDay), grid.VisibleEndExclusive);
        Assert.Equal(grid.VisibleStart.AddDays(CalendarMonth.CellCount), grid.VisibleEndExclusive);
        Assert.Equal(grid.VisibleEndExclusive.AddDays(-1), grid.Days[^1].Date);
        Assert.True(grid.Days.All(day => day.Date >= grid.VisibleStart && day.Date < grid.VisibleEndExclusive));
        Assert.Equal(CalendarMonth.CellCount, grid.Days.Select(day => day.Date).Distinct().Count());
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, 2026, 11, 29, 2027, 1, 10)]
    [InlineData(DayOfWeek.Monday, 2026, 11, 30, 2027, 1, 11)]
    public void December2026_VisibleRangeSpansTheYearBoundary(
        DayOfWeek weekStart,
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay)
    {
        var grid = new CalendarMonth(2026, 12, weekStart);

        Assert.Equal(TestDates.Day(startYear, startMonth, startDay), grid.VisibleStart);
        Assert.Equal(TestDates.Day(endYear, endMonth, endDay), grid.VisibleEndExclusive);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void CurrentMonthFlags_MarkOnlyDaysOfTheDisplayedMonth(DayOfWeek weekStart)
    {
        var grid = new CalendarMonth(2026, 11, weekStart);

        var current = grid.Days.Where(day => day.IsCurrentMonth).ToArray();

        Assert.Equal(30, current.Length);
        Assert.True(current.All(day => day.Date.Year == 2026 && day.Date.Month == 11));
        Assert.True(grid.Days.Where(day => !day.IsCurrentMonth)
            .All(day => day.Date.Year != 2026 || day.Date.Month != 11));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void February2028_LeapGridHasTwentyNineCurrentMonthDays(DayOfWeek weekStart)
    {
        var grid = new CalendarMonth(2028, 2, weekStart);

        Assert.Equal(42, grid.Days.Count);
        Assert.Equal(29, grid.Days.Count(day => day.IsCurrentMonth));
        Assert.True(grid.Days.Any(day => day.Date == TestDates.Day(2028, 2, 29) && day.IsCurrentMonth));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday, 2028, 1, 30, 2028, 3, 12)]
    [InlineData(DayOfWeek.Monday, 2028, 1, 31, 2028, 3, 13)]
    public void February2028_VisibleRangeCoversTheLeapDayGrid(
        DayOfWeek weekStart,
        int startYear,
        int startMonth,
        int startDay,
        int endYear,
        int endMonth,
        int endDay)
    {
        var grid = new CalendarMonth(2028, 2, weekStart);

        Assert.Equal(TestDates.Day(startYear, startMonth, startDay), grid.VisibleStart);
        Assert.Equal(TestDates.Day(endYear, endMonth, endDay), grid.VisibleEndExclusive);
        Assert.True(grid.VisibleStart <= TestDates.Day(2028, 2, 29));
        Assert.True(TestDates.Day(2028, 2, 29) < grid.VisibleEndExclusive);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void December2026_NextIsJanuary2027(DayOfWeek weekStart)
    {
        var december = new CalendarMonth(2026, 12, weekStart);
        var january = december.Next();

        Assert.Equal(2027, january.Year);
        Assert.Equal(1, january.Month);
        Assert.Equal(weekStart, january.WeekStart);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void January2027_PreviousIsDecember2026(DayOfWeek weekStart)
    {
        var january = new CalendarMonth(2027, 1, weekStart);
        var december = january.Previous();

        Assert.Equal(2026, december.Year);
        Assert.Equal(12, december.Month);
        Assert.Equal(weekStart, december.WeekStart);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void Navigation_RoundTripsAcross2024To2028(DayOfWeek weekStart)
    {
        for (var year = 2024; year <= 2028; year++)
        {
            for (var month = 1; month <= 12; month++)
            {
                var current = new CalendarMonth(year, month, weekStart);
                var next = current.Next();
                var expectedNext = month == 12 ? (Year: year + 1, Month: 1) : (Year: year, Month: month + 1);

                Assert.Equal(expectedNext.Year, next.Year);
                Assert.Equal(expectedNext.Month, next.Month);

                var roundTrip = next.Previous();
                Assert.Equal(year, roundTrip.Year);
                Assert.Equal(month, roundTrip.Month);
                Assert.Equal(weekStart, roundTrip.WeekStart);
                Assert.Equal(current.VisibleStart, roundTrip.VisibleStart);
                Assert.Equal(current.VisibleEndExclusive, roundTrip.VisibleEndExclusive);

                var back = current.Previous().Next();
                Assert.Equal(year, back.Year);
                Assert.Equal(month, back.Month);
                Assert.Equal(weekStart, back.WeekStart);
            }
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void Weeks_AreSixRowsOfSevenDaysInReadingOrder(DayOfWeek weekStart)
    {
        var grid = new CalendarMonth(2026, 11, weekStart);

        Assert.Equal(7, WeekRow.DayCount);
        Assert.Equal(6, grid.Weeks.Count);

        for (var row = 0; row < grid.Weeks.Count; row++)
        {
            var week = grid.Weeks[row];
            Assert.Equal(WeekRow.DayCount, week.Days.Count);

            for (var column = 0; column < WeekRow.DayCount; column++)
            {
                Assert.Equal(grid.Days[(row * WeekRow.DayCount) + column], week.Days[column]);
            }
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void Weeks_IsoWeekNumberMatchesTheMondayCell(DayOfWeek weekStart)
    {
        var months = new[]
        {
            new CalendarMonth(2026, 11, weekStart),
            new CalendarMonth(2026, 12, weekStart),
            new CalendarMonth(2028, 2, weekStart),
        };

        foreach (var grid in months)
        {
            foreach (var week in grid.Weeks)
            {
                var monday = week.Days.Single(day => day.Date.DayOfWeek == DayOfWeek.Monday);
                var expected = ISOWeek.GetWeekOfYear(monday.Date.ToDateTime(TimeOnly.MinValue));

                Assert.Equal(expected, week.IsoWeekNumber);
            }
        }
    }

    [Fact]
    public void November2026_SundayStart_HasExpectedIsoWeeks()
    {
        var grid = new CalendarMonth(2026, 11, DayOfWeek.Sunday);

        Assert.Equal(new[] { 45, 46, 47, 48, 49, 50 }, grid.Weeks.Select(week => week.IsoWeekNumber).ToArray());
    }

    [Fact]
    public void November2026_MondayStart_HasExpectedIsoWeeks()
    {
        var grid = new CalendarMonth(2026, 11, DayOfWeek.Monday);

        Assert.Equal(new[] { 44, 45, 46, 47, 48, 49 }, grid.Weeks.Select(week => week.IsoWeekNumber).ToArray());
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void December2026_ContainsIsoWeek53(DayOfWeek weekStart)
    {
        var grid = new CalendarMonth(2026, 12, weekStart);

        Assert.Contains(53, grid.Weeks.Select(week => week.IsoWeekNumber));

        var week53 = grid.Weeks.Single(week => week.IsoWeekNumber == 53);
        Assert.True(week53.Days.Any(day => day.Date == TestDates.Day(2026, 12, 28)));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void December2026_LastGridRowIsFirstWeekOf2027(DayOfWeek weekStart)
    {
        var grid = new CalendarMonth(2026, 12, weekStart);

        Assert.Equal(1, grid.Weeks[^1].IsoWeekNumber);
        Assert.Equal(ISOWeek.GetWeekOfYear(TestDates.Day(2027, 1, 4).ToDateTime(TimeOnly.MinValue)), grid.Weeks[^1].IsoWeekNumber);
    }

    [Fact]
    public void January2027_MondayStart_FirstGridRowIsWeek53Of2026()
    {
        var grid = new CalendarMonth(2027, 1, DayOfWeek.Monday);

        Assert.Equal(53, grid.Weeks[0].IsoWeekNumber);
        Assert.Equal(TestDates.Day(2026, 12, 28), grid.Weeks[0].Days[0].Date);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void WithEventCounts_AppliesCountsAndZeroesOtherDays(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);
        var counts = new Dictionary<DateOnly, int>
        {
            [TestDates.Day(2026, 11, 1)] = 1,
            [TestDates.Day(2026, 11, 2)] = 3,
        };

        var withCounts = month.WithEventCounts(counts);

        Assert.Equal(1, withCounts.Days.Single(day => day.Date == TestDates.Day(2026, 11, 1)).EventCount);
        Assert.Equal(3, withCounts.Days.Single(day => day.Date == TestDates.Day(2026, 11, 2)).EventCount);
        Assert.True(withCounts.Days.Single(day => day.Date == TestDates.Day(2026, 11, 2)).HasEvents);

        foreach (var day in withCounts.Days)
        {
            if (day.Date != TestDates.Day(2026, 11, 1) && day.Date != TestDates.Day(2026, 11, 2))
            {
                Assert.Equal(0, day.EventCount);
                Assert.False(day.HasEvents);
            }
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void WithEventCounts_PreservesMonthIdentityAndDayFlags(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);
        var counts = new Dictionary<DateOnly, int>
        {
            [TestDates.Day(2026, 11, 15)] = 2,
        };

        var withCounts = month.WithEventCounts(counts);

        Assert.Equal(month.Year, withCounts.Year);
        Assert.Equal(month.Month, withCounts.Month);
        Assert.Equal(month.WeekStart, withCounts.WeekStart);
        Assert.Equal(month.Title, withCounts.Title);
        Assert.Equal(month.AbbreviatedTitle, withCounts.AbbreviatedTitle);
        Assert.Equal(month.VisibleStart, withCounts.VisibleStart);
        Assert.Equal(month.VisibleEndExclusive, withCounts.VisibleEndExclusive);
        Assert.Equal(month.Days.Count, withCounts.Days.Count);

        for (var index = 0; index < month.Days.Count; index++)
        {
            Assert.Equal(month.Days[index].Date, withCounts.Days[index].Date);
            Assert.Equal(month.Days[index].IsToday, withCounts.Days[index].IsToday);
            Assert.Equal(month.Days[index].IsCurrentMonth, withCounts.Days[index].IsCurrentMonth);
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void WithEventCounts_RebuildsWeekRowsFromTheUpdatedDays(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);
        var countedDate = TestDates.Day(2026, 11, 10);
        var withCounts = month.WithEventCounts(new Dictionary<DateOnly, int>
        {
            [countedDate] = 4,
        });

        Assert.Equal(6, withCounts.Weeks.Count);
        Assert.Equal(4, withCounts.Days.Single(day => day.Date == countedDate).EventCount);
        Assert.Equal(4, withCounts.Weeks.SelectMany(week => week.Days).Single(day => day.Date == countedDate).EventCount);

        for (var row = 0; row < withCounts.Weeks.Count; row++)
        {
            Assert.Equal(WeekRow.DayCount, withCounts.Weeks[row].Days.Count);
            Assert.Equal(month.Weeks[row].IsoWeekNumber, withCounts.Weeks[row].IsoWeekNumber);
        }
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void WithEventCounts_IgnoresDatesOutsideTheVisibleRange(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);

        var withCounts = month.WithEventCounts(new Dictionary<DateOnly, int>
        {
            [TestDates.Day(2027, 6, 1)] = 5,
            [TestDates.Day(2025, 1, 1)] = 2,
        });

        Assert.Equal(CalendarMonth.CellCount, withCounts.Days.Count);
        Assert.True(withCounts.Days.All(day => day.EventCount == 0));
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void WithEventCounts_DoesNotMutateTheOriginalMonth(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);
        var countedDate = TestDates.Day(2026, 11, 3);

        _ = month.WithEventCounts(new Dictionary<DateOnly, int> { [countedDate] = 7 });

        Assert.Equal(0, month.Days.Single(day => day.Date == countedDate).EventCount);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void WithEventCounts_ThrowsForNullCounts(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);

        Assert.Throws<ArgumentNullException>(() => { _ = month.WithEventCounts(null!); });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Constructor_RejectsInvalidMonth(int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = new CalendarMonth(2026, month); });
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void Titles_ContainTheDisplayedYear(DayOfWeek weekStart)
    {
        var month = new CalendarMonth(2026, 11, weekStart);

        Assert.Contains("2026", month.Title);
        Assert.Contains("2026", month.AbbreviatedTitle);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    public void ForToday_ReturnsTheMonthContainingToday(DayOfWeek weekStart)
    {
        var before = DateOnly.FromDateTime(DateTime.Now);
        var month = CalendarMonth.ForToday(weekStart);
        var after = DateOnly.FromDateTime(DateTime.Now);

        // Do not assert a specific date if the run straddles a date change.
        if (before != after)
        {
            return;
        }

        Assert.Equal(before.Year, month.Year);
        Assert.Equal(before.Month, month.Month);
        Assert.Equal(weekStart, month.WeekStart);
        Assert.Equal(CalendarMonth.CellCount, month.Days.Count);
        Assert.Equal(1, month.Days.Count(day => day.IsToday));
        Assert.True(month.Days.Any(day => day.Date == before && day.IsToday));
    }
}

