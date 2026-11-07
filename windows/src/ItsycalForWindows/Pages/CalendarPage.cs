using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using ItsycalForWindows.Models;
using ItsycalForWindows.Services;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ItsycalForWindows.Pages;

/// <summary>
/// Calendar flyout: an Adaptive Card with the month grid (today highlighted, adjacent-month
/// days dimmed, optional ISO week numbers), month navigation, and the events of the visible
/// range. The page never blocks: <see cref="GetContent"/> returns the card immediately and
/// the events load in the background, after which the card is refreshed.
/// </summary>
internal sealed partial class CalendarPage : ContentPage
{
    // Re-reading the event store on every flyout open keeps the month current, while the
    // short lifetime prevents a render/load feedback loop when the card refreshes itself.
    private static readonly TimeSpan EventsRefreshInterval = TimeSpan.FromSeconds(30);

    private const string PreviousMonthAction = "prev";
    private const string TodayAction = "today";
    private const string NextMonthAction = "next";

    // The extension ships English UI strings only (windows/README.md, Limitations).
    private const string WeekNumberHeader = "Wk";
    private const string EventsHeading = "Events";
    private const string NoEventsLabel = "No events in this view.";
    private const string LoadingEventsLabel = "Loading events\u2026";
    private const string PreviousMonthLabel = "Previous month";
    private const string TodayLabel = "Today";
    private const string NextMonthLabel = "Next month";
    private const string AllDayLabel = "All day";
    private const string EventSeparator = " \u00b7 ";
    private const char EventDot = '\u2022';

    private readonly object _sync = new();
    private readonly SettingsManager _settings;
    private readonly IEventSource _eventSource;

    private CalendarMonth _month;
    private IReadOnlyList<CalendarEvent> _events = Array.Empty<CalendarEvent>();
    private DateOnly? _loadedFrom;
    private DateOnly? _loadedToExclusive;
    private DateTime _loadedAtUtc;
    private DateOnly? _loadingFrom;
    private DateOnly? _loadingToExclusive;
    private bool _isLoading;
    private int _loadGeneration;

    // The form the host is currently showing (if any) and the card JSON it must show.
    private CalendarForm? _currentForm;
    private string _renderedTemplate = string.Empty;

    // The card is only rebuilt when something that affects it changed.
    private CalendarMonth? _renderedMonth;
    private IReadOnlyList<CalendarEvent>? _renderedEvents;
    private bool _renderedLoading;
    private bool _renderedWeekNumbers;

    public CalendarPage(SettingsManager settings, IEventSource eventSource)
        : this(settings, eventSource, CalendarMonth.ForToday(settings.WeekStart))
    {
    }

    public CalendarPage(SettingsManager settings, IEventSource eventSource, CalendarMonth month)
    {
        _settings = settings;
        _eventSource = eventSource;
        _month = month;

        Name = "Open";
        Title = month.Title;
        Icon = new IconInfo("\uE787"); // Segoe Fluent: Calendar

        Render(startEventLoad: false);
    }

    /// <summary>The month currently displayed; "Today" resets it to the current month.</summary>
    public CalendarMonth Month
    {
        get
        {
            lock (_sync)
            {
                return _month;
            }
        }
    }

    public override IContent[] GetContent()
    {
        Render(startEventLoad: true);

        // A fresh form object per fetch: the host subscribes to a form's property changes
        // when it builds the card view model, so a discarded form becomes collectable with
        // its subscription instead of accumulating subscriptions on one long-lived object
        // every time the dock flyout is reopened.
        CalendarForm form;
        lock (_sync)
        {
            form = new CalendarForm { TemplateJson = _renderedTemplate };
            _currentForm = form;
        }

        return [form];
    }

    public override ICommandResult SubmitForm(string inputs) => HandleFormSubmit(inputs, data: null);

    public override ICommandResult SubmitForm(string inputs, string data) => HandleFormSubmit(inputs, data);

    /// <summary>
    /// Handles the card's Action.Submit buttons. The host passes the action's <c>data</c>
    /// object as the second argument (the installed FormContent API exposes
    /// <c>SubmitForm(inputs, data)</c>); reading the same key from <paramref name="inputs"/>
    /// as a fallback keeps navigation working if a host merges the action data into the
    /// input payload instead.
    /// </summary>
    private CommandResult HandleFormSubmit(string inputs, string? data)
    {
        var action = ReadNavigationAction(data) ?? ReadNavigationAction(inputs);
        var current = Month;
        switch (action)
        {
            case PreviousMonthAction:
                Navigate(current.Previous());
                break;
            case NextMonthAction:
                Navigate(current.Next());
                break;
            case TodayAction:
                Navigate(CalendarMonth.ForToday(_settings.WeekStart));
                break;
        }

        return CommandResult.KeepOpen();
    }

    private void Navigate(CalendarMonth month)
    {
        lock (_sync)
        {
            _month = month;
            ResetEventsLocked();
        }

        Title = month.Title;
        Render(startEventLoad: true);
    }

    /// <summary>
    /// Rebuilds the card when the displayed month, the cached events, the loading state or
    /// the week-number setting changed, and starts a background event load when the visible
    /// range is not cached or is stale. Cheap when nothing changed: no I/O, no JSON build.
    /// </summary>
    private void Render(bool startEventLoad)
    {
        CalendarMonth month;
        IReadOnlyList<CalendarEvent> events;
        bool isLoading;
        bool startLoad = false;
        DateOnly loadFrom;
        DateOnly loadTo;
        var loadGeneration = 0;
        CalendarForm? formToRefresh = null;
        string? templateToPublish = null;

        lock (_sync)
        {
            EnsureWeekStartLocked();

            loadFrom = _month.VisibleStart;
            loadTo = _month.VisibleEndExclusive;
            if (startEventLoad && NeedsEventLoadLocked(loadFrom, loadTo))
            {
                _isLoading = true;
                _loadingFrom = loadFrom;
                _loadingToExclusive = loadTo;
                _loadGeneration++;
                loadGeneration = _loadGeneration;
                startLoad = true;
            }

            month = _month;
            events = _events;
            isLoading = _isLoading;

            var showWeekNumbers = _settings.ShowWeekNumbers;
            var showLoading = isLoading && events.Count == 0;
            if (!ReferenceEquals(_renderedMonth, month)
                || !ReferenceEquals(_renderedEvents, events)
                || _renderedLoading != showLoading
                || _renderedWeekNumbers != showWeekNumbers)
            {
                _renderedMonth = month;
                _renderedEvents = events;
                _renderedLoading = showLoading;
                _renderedWeekNumbers = showWeekNumbers;
                _renderedTemplate = BuildTemplateJson(month, events, showWeekNumbers, showLoading);
                templateToPublish = _renderedTemplate;
                formToRefresh = _currentForm;
            }
        }

        if (templateToPublish is not null && formToRefresh is not null)
        {
            // FormContent raises a property change from the TemplateJson setter, which makes
            // the host re-render the card without fetching the page content again.
            formToRefresh.TemplateJson = templateToPublish;
        }

        if (!string.Equals(Title, month.Title, StringComparison.Ordinal))
        {
            Title = month.Title;
        }

        if (IsLoading != isLoading)
        {
            IsLoading = isLoading;
        }

        if (startLoad)
        {
            _ = LoadEventsAsync(loadFrom, loadTo, loadGeneration);
        }
    }

    private async Task LoadEventsAsync(DateOnly fromInclusive, DateOnly toExclusive, int generation)
    {
        IReadOnlyList<CalendarEvent> events;
        try
        {
            events = await _eventSource.GetEventsAsync(fromInclusive, toExclusive).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // IEventSource must not throw for an unavailable calendar, but the calendar UI
            // must still work if an implementation does: fall back to no events.
            events = Array.Empty<CalendarEvent>();
        }

        lock (_sync)
        {
            if (generation != _loadGeneration)
            {
                // The month changed while this load was in flight; its result is stale.
                return;
            }

            _events = SortEvents(events);
            _loadedFrom = fromInclusive;
            _loadedToExclusive = toExclusive;
            _loadedAtUtc = DateTime.UtcNow;
            _isLoading = false;
            _loadingFrom = null;
            _loadingToExclusive = null;
        }

        try
        {
            Render(startEventLoad: false);
        }
        catch (Exception)
        {
            // The host may have torn the page down while the events were loading; there is
            // nothing left to refresh in that case.
        }
    }

    private void EnsureWeekStartLocked()
    {
        var weekStart = _settings.WeekStart;
        if (_month.WeekStart == weekStart)
        {
            return;
        }

        // The settings page can change the first day of the week while this page is alive;
        // rebuild the grid and reload events because the visible range moves by one day.
        _month = new CalendarMonth(_month.Year, _month.Month, weekStart);
        ResetEventsLocked();
    }

    private void ResetEventsLocked()
    {
        _events = Array.Empty<CalendarEvent>();
        _loadedFrom = null;
        _loadedToExclusive = null;
        _loadingFrom = null;
        _loadingToExclusive = null;
        _isLoading = false;
        _loadGeneration++;
    }

    private bool NeedsEventLoadLocked(DateOnly fromInclusive, DateOnly toExclusive)
    {
        if (_isLoading && _loadingFrom == fromInclusive && _loadingToExclusive == toExclusive)
        {
            return false;
        }

        if (_loadedFrom == fromInclusive
            && _loadedToExclusive == toExclusive
            && DateTime.UtcNow - _loadedAtUtc < EventsRefreshInterval)
        {
            return false;
        }

        return true;
    }

    private static IReadOnlyList<CalendarEvent> SortEvents(IReadOnlyList<CalendarEvent> events)
    {
        if (events.Count <= 1)
        {
            return events;
        }

        return events
            .OrderBy(item => item.Start)
            .ThenBy(item => item.DisplayTitle, StringComparer.CurrentCulture)
            .ToArray();
    }

    private static string? ReadNavigationAction(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(payload) is JsonObject json
                && json["nav"] is JsonValue value
                && value.TryGetValue<string>(out var action)
                ? action
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildTemplateJson(
        CalendarMonth month,
        IReadOnlyList<CalendarEvent> events,
        bool showWeekNumbers,
        bool showLoading)
    {
        var eventCounts = BuildEventCounts(events, month.VisibleStart, month.VisibleEndExclusive);
        var gridMonth = eventCounts.Count == 0 ? month : month.WithEventCounts(eventCounts);

        var body = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = Literal(month.Title),
                ["size"] = "large",
                ["weight"] = "bolder",
                ["style"] = "heading",
                ["horizontalAlignment"] = "center",
                ["wrap"] = true,
            },
            BuildWeekdayHeader(gridMonth.WeekStart, showWeekNumbers),
        };

        foreach (var week in gridMonth.Weeks)
        {
            body.Add(BuildWeekRow(week, showWeekNumbers));
        }

        AddEventsSection(body, events, showLoading);

        var card = new JsonObject
        {
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            // 1.5 is the version the host itself renders for its own cards; the elements
            // used here (TextBlock/ColumnSet/Action.Submit, heading style) need no later one.
            ["version"] = "1.5",
            ["body"] = body,
            ["actions"] = new JsonArray
            {
                BuildNavigationAction(PreviousMonthLabel, PreviousMonthAction, "nav-previous"),
                BuildNavigationAction(TodayLabel, TodayAction, "nav-today"),
                BuildNavigationAction(NextMonthLabel, NextMonthAction, "nav-next"),
            },
        };

        return card.ToJsonString();
    }

    private static JsonObject BuildWeekdayHeader(DayOfWeek weekStart, bool showWeekNumbers)
    {
        var columns = new JsonArray();
        if (showWeekNumbers)
        {
            columns.Add(BuildColumn("auto", TextBlock(WeekNumberHeader, color: "light", weight: "bolder", alignment: "center")));
        }

        var shortestDayNames = CultureInfo.CurrentCulture.DateTimeFormat.ShortestDayNames;
        for (var column = 0; column < WeekRow.DayCount; column++)
        {
            var dayOfWeek = (DayOfWeek)(((int)weekStart + column) % WeekRow.DayCount);
            columns.Add(BuildColumn("stretch", TextBlock(shortestDayNames[(int)dayOfWeek], color: "light", weight: "bolder", alignment: "center")));
        }

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["columns"] = columns,
        };
    }

    private static JsonObject BuildWeekRow(WeekRow week, bool showWeekNumbers)
    {
        var columns = new JsonArray();
        if (showWeekNumbers)
        {
            columns.Add(BuildColumn(
                "auto",
                TextBlock(week.IsoWeekNumber.ToString(CultureInfo.CurrentCulture), color: "light", alignment: "center")));
        }

        foreach (var day in week.Days)
        {
            string? color = null;
            if (day.IsToday)
            {
                color = "accent";
            }
            else if (!day.IsCurrentMonth)
            {
                color = "light";
            }

            var text = day.HasEvents
                ? day.Date.Day.ToString(CultureInfo.CurrentCulture) + EventDot
                : day.Date.Day.ToString(CultureInfo.CurrentCulture);
            var weight = day.IsToday ? "bolder" : null;

            columns.Add(BuildColumn("stretch", TextBlock(text, color: color, weight: weight, alignment: "center")));
        }

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["columns"] = columns,
        };
    }

    private static void AddEventsSection(JsonArray body, IReadOnlyList<CalendarEvent> events, bool showLoading)
    {
        body.Add(new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = EventsHeading,
            ["size"] = "medium",
            ["weight"] = "bolder",
            ["spacing"] = "medium",
            ["wrap"] = true,
        });

        if (showLoading)
        {
            body.Add(TextBlock(LoadingEventsLabel, color: "light", wrap: true));
            return;
        }

        if (events.Count == 0)
        {
            body.Add(TextBlock(NoEventsLabel, color: "light", wrap: true));
            return;
        }

        foreach (var item in events)
        {
            body.Add(TextBlock(FormatEventLine(item), wrap: true));
        }
    }

    private static string FormatEventLine(CalendarEvent item)
    {
        var title = item.DisplayTitle;
        var culture = CultureInfo.CurrentCulture;
        if (item.IsAllDay)
        {
            return $"{FormatDateRange(item.StartDate, item.EndDateInclusive)}"
                + $"{EventSeparator}{AllDayLabel}{EventSeparator}{title}";
        }

        if (item.IsMultiDay)
        {
            return $"{FormatDateRange(item.StartDate, item.EndDateInclusive)}"
                + $"{EventSeparator}{item.Start.LocalDateTime.ToString("t", culture)}{EventSeparator}{title}";
        }

        return $"{item.Start.LocalDateTime.ToString("t", culture)}{EventSeparator}{title}";
    }

    private static string FormatDateRange(DateOnly start, DateOnly end)
    {
        var culture = CultureInfo.CurrentCulture;
        return start == end
            ? start.ToString("MMM d", culture)
            : $"{start.ToString("MMM d", culture)}\u2013{end.ToString("MMM d", culture)}";
    }

    /// <summary>
    /// Counts, for every visible day, how many events overlap it. An event covers its start
    /// day through its inclusive end day (<see cref="CalendarEvent.EndDateInclusive"/>),
    /// clamped to the visible range.
    /// </summary>
    private static Dictionary<DateOnly, int> BuildEventCounts(
        IReadOnlyList<CalendarEvent> events,
        DateOnly fromInclusive,
        DateOnly toExclusive)
    {
        var counts = new Dictionary<DateOnly, int>();
        if (events.Count == 0)
        {
            return counts;
        }

        var lastVisibleDay = toExclusive.AddDays(-1);
        foreach (var item in events)
        {
            var start = item.StartDate < fromInclusive ? fromInclusive : item.StartDate;
            var end = item.EndDateInclusive > lastVisibleDay ? lastVisibleDay : item.EndDateInclusive;
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                counts[day] = counts.TryGetValue(day, out var count) ? count + 1 : 1;
            }
        }

        return counts;
    }

    private static JsonObject BuildNavigationAction(string title, string action, string id) => new()
    {
        ["type"] = "Action.Submit",
        ["id"] = id,
        ["title"] = title,
        ["data"] = new JsonObject
        {
            ["nav"] = action,
        },
    };

    private static JsonObject BuildColumn(string width, JsonObject content) => new()
    {
        ["type"] = "Column",
        ["width"] = width,
        ["items"] = new JsonArray { content },
    };

    private static JsonObject TextBlock(
        string text,
        string? color = null,
        string? weight = null,
        string? alignment = null,
        bool wrap = false)
    {
        var block = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = Literal(text),
            ["wrap"] = wrap,
        };

        if (color is not null)
        {
            block["color"] = color;
        }

        if (weight is not null)
        {
            block["weight"] = weight;
        }

        if (alignment is not null)
        {
            block["horizontalAlignment"] = alignment;
        }

        return block;
    }

    /// <summary>
    /// Neutralizes Adaptive Card template placeholders (<c>${...}</c>) inside dynamic text.
    /// The host expands the card with the Adaptive Cards template engine, so an event title
    /// that happens to contain <c>${</c> must be treated as data, not as a binding.
    /// </summary>
    private static string Literal(string text) => text.Replace("${", "$ {", StringComparison.Ordinal);
}

/// <summary>
/// The single Adaptive Card the calendar page renders. The host subscribes to the form's
/// property changes and re-renders when <see cref="FormContent.TemplateJson"/> changes, so
/// navigation and background event loading only need to update the template.
/// </summary>
internal sealed partial class CalendarForm : FormContent
{
    public CalendarForm()
    {
        // Binding data is unused; an empty object keeps the host's template expansion well
        // defined even though the card contains no ${...} placeholders.
        DataJson = "{}";
    }
}
