using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ItsycalForWindows.Models;
using ItsycalForWindows.Services;

namespace ItsycalForWindows.Pages;

/// <summary>
/// Calendar flyout: a month grid with today highlighted, month navigation, and the events
/// of the visible range. Rendering is intentionally simple in the bootstrap revision;
/// the adaptive-card grid and day interaction land with the calendar UI feature.
/// </summary>
internal sealed partial class CalendarPage : ContentPage
{
    private readonly CalendarMonth _month;
    private readonly SettingsManager _settings;

    public CalendarPage(SettingsManager settings, IEventSource eventSource)
        : this(settings, eventSource, CalendarMonth.ForToday(settings.WeekStart))
    {
    }

    public CalendarPage(SettingsManager settings, IEventSource eventSource, CalendarMonth month)
    {
        _settings = settings;
        _month = month;

        Name = "Open";
        Title = month.Title;
        Icon = new IconInfo("\uE787");
    }

    public CalendarMonth Month => _month;

    public override IContent[] GetContent()
    {
        var lines = new List<string>
        {
            $"# {_month.Title}",
            string.Empty,
        };

        if (_settings.ShowWeekNumbers)
        {
            lines.Add("_ISO week numbers enabled in settings._");
            lines.Add(string.Empty);
        }

        foreach (var week in _month.Days.Chunk(7))
        {
            lines.Add(string.Join(
                " | ",
                week.Select(day => day.IsToday ? $"[{day.Date.Day}]" : day.Date.Day.ToString(System.Globalization.CultureInfo.CurrentCulture))));
        }

        lines.Add(string.Empty);
        lines.Add("_Events and navigation land in the calendar features._");

        return [new MarkdownContent(string.Join("\n", lines))];
    }
}
