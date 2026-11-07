using System;
using System.Collections.Generic;
using System.Globalization;

namespace ItsycalForWindows.Models;

/// <summary>
/// A displayed month: the month identity, its 42-cell grid, and the visible date range.
/// Pure data and arithmetic — no UI and no I/O; the clock is read only in
/// <see cref="ForToday"/>.
/// </summary>
public sealed class CalendarMonth
{
    /// <summary>Cells in a month grid: 6 rows x 7 days.</summary>
    public const int CellCount = 42;

    private readonly DayOfWeek _weekStart;
    private readonly IReadOnlyList<CalendarDay> _days;

    public CalendarMonth(int year, int month, DayOfWeek weekStart = DayOfWeek.Sunday)
        : this(year, month, weekStart, daysOverride: null)
    {
    }

    private CalendarMonth(int year, int month, DayOfWeek weekStart, IReadOnlyList<CalendarDay>? daysOverride)
    {
        if (month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        Year = year;
        Month = month;
        _weekStart = weekStart;

        var firstDay = new DateOnly(year, month, 1);
        var leading = ((int)firstDay.DayOfWeek - (int)weekStart + 7) % 7;
        VisibleStart = firstDay.AddDays(-leading);
        VisibleEndExclusive = VisibleStart.AddDays(CellCount);
        Title = firstDay.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
        AbbreviatedTitle = firstDay.ToString("MMM yyyy", CultureInfo.CurrentCulture);

        if (daysOverride is not null)
        {
            _days = daysOverride;
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var days = new List<CalendarDay>(CellCount);
        for (var i = 0; i < CellCount; i++)
        {
            var date = VisibleStart.AddDays(i);
            days.Add(new CalendarDay(
                date,
                IsToday: date == today,
                IsCurrentMonth: date.Year == year && date.Month == month));
        }

        _days = days;
    }

    public int Year { get; }

    public int Month { get; }

    /// <summary>Human-readable month title, for example "November 2026".</summary>
    public string Title { get; }

    /// <summary>Compact month title, for example "Nov 2026".</summary>
    public string AbbreviatedTitle { get; }

    /// <summary>First visible date (may belong to the previous month).</summary>
    public DateOnly VisibleStart { get; }

    /// <summary>First date after the visible range.</summary>
    public DateOnly VisibleEndExclusive { get; }

    /// <summary>Exactly 42 grid cells in reading order.</summary>
    public IReadOnlyList<CalendarDay> Days => _days;

    public DayOfWeek WeekStart => _weekStart;

    public CalendarMonth Previous() => Month == 1
        ? new CalendarMonth(Year - 1, 12, _weekStart)
        : new CalendarMonth(Year, Month - 1, _weekStart);

    public CalendarMonth Next() => Month == 12
        ? new CalendarMonth(Year + 1, 1, _weekStart)
        : new CalendarMonth(Year, Month + 1, _weekStart);

    /// <summary>The month containing the current local date.</summary>
    public static CalendarMonth ForToday(DayOfWeek weekStart = DayOfWeek.Sunday)
    {
        var today = DateTime.Now;
        return new CalendarMonth(today.Year, today.Month, weekStart);
    }

    /// <summary>
    /// Returns the same month with event counts applied to matching dates. Dates absent
    /// from <paramref name="countsByDate"/> get a zero count. Other cell flags are kept.
    /// </summary>
    public CalendarMonth WithEventCounts(IReadOnlyDictionary<DateOnly, int> countsByDate)
    {
        ArgumentNullException.ThrowIfNull(countsByDate);

        var days = new List<CalendarDay>(_days.Count);
        foreach (var day in _days)
        {
            var count = countsByDate.TryGetValue(day.Date, out var value) ? value : 0;
            days.Add(day with { EventCount = count });
        }

        return new CalendarMonth(Year, Month, _weekStart, days);
    }
}
