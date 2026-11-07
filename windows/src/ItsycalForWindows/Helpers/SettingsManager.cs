using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ItsycalForWindows;

/// <summary>
/// Extension settings, persisted by the Command Palette host through
/// <see cref="JsonSettingsManager"/>. Pages read the exposed properties; they never touch
/// the underlying settings objects.
/// </summary>
internal sealed partial class SettingsManager : JsonSettingsManager
{
    private static readonly string _namespace = "ItsycalForWindows";

    /// <summary>
    /// Date format used by the dock band button. The value is a .NET date format string chosen
    /// from <see cref="DockDateFormats"/>; the first entry is the default and the fallback for
    /// values a previous build may have persisted.
    /// </summary>
    internal const string DefaultDockDateFormat = "ddd d MMM";

    /// <summary>Supported dock date formats, in the order shown on the settings page.</summary>
    private static readonly string[] _dockDateFormats =
    [
        DefaultDockDateFormat,
        "d MMM",
        "dd.MM.yyyy",
        "MMM d",
    ];

    private readonly ToggleSetting _weekStartsOnMonday = new(
        Namespaced(nameof(WeekStartsOnMonday)),
        "Week starts on Monday",
        "Use Monday as the first day of the week in the calendar grid.",
        CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday);

    private readonly ToggleSetting _showWeekNumbers = new(
        Namespaced(nameof(ShowWeekNumbers)),
        "Show ISO week numbers",
        "Show a week-number column in the month grid.",
        false);

    private readonly ChoiceSetSetting _dockDateFormat = new(
        Namespaced(nameof(DockDateFormat)),
        "Dock date format",
        "How the current date is formatted on the dock band button.",
        BuildDockDateFormatChoices());

    private readonly ToggleSetting _showEventIndicator = new(
        Namespaced(nameof(ShowEventIndicator)),
        "Show today's event count",
        "Show the number of events today in the dock band subtitle.",
        true);

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        // A value written by an older or newer build that is not one of the choices must not
        // replace the default format; the dock band would otherwise show raw format text.
        _dockDateFormat.IgnoreUnknownValue = true;

        Settings.Add(_weekStartsOnMonday);
        Settings.Add(_showWeekNumbers);
        Settings.Add(_dockDateFormat);
        Settings.Add(_showEventIndicator);

        // Load settings from file upon initialization. A missing, unreadable or corrupt
        // settings file must never prevent the extension from starting.
        try
        {
            LoadSettings();
        }
        catch (Exception)
        {
        }

        Settings.SettingsChanged += (_, _) =>
        {
            try
            {
                SaveSettings();
            }
            catch (Exception)
            {
            }
        };
    }

    /// <summary>True when the grid should start weeks on Monday.</summary>
    public bool WeekStartsOnMonday => _weekStartsOnMonday.Value;

    /// <summary>First day of the week used by the calendar grid.</summary>
    public DayOfWeek WeekStart => WeekStartsOnMonday ? DayOfWeek.Monday : DayOfWeek.Sunday;

    /// <summary>True when the grid should show ISO week numbers.</summary>
    public bool ShowWeekNumbers => _showWeekNumbers.Value;

    /// <summary>True when the dock band subtitle should show today's event count.</summary>
    public bool ShowEventIndicator => _showEventIndicator.Value;

    /// <summary>
    /// Date format applied to the dock band title. A persisted value that is not one of the
    /// supported choices (for example, written by a future build) falls back to the default
    /// instead of throwing out of <see cref="DateTime.ToString(string, IFormatProvider)"/>.
    /// </summary>
    public string DockDateFormat
    {
        get
        {
            var value = _dockDateFormat.Value;
            return value is DefaultDockDateFormat
                or "d MMM"
                or "dd.MM.yyyy"
                or "MMM d"
                ? value
                : DefaultDockDateFormat;
        }
    }

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    private static List<ChoiceSetSetting.Choice> BuildDockDateFormatChoices()
    {
        var choices = new List<ChoiceSetSetting.Choice>(_dockDateFormats.Length);
        foreach (var format in _dockDateFormats)
        {
            // The format string is both the stored value and the label: the settings page is
            // where a user picks an exact pattern, so showing the pattern is unambiguous.
            choices.Add(new ChoiceSetSetting.Choice(format, format));
        }

        return choices;
    }

    internal static string SettingsJsonPath()
    {
        // The host runs the extension inside a package identity whose LocalApplicationData
        // folder may not be writable. Settings are a convenience: fall back to the temp
        // folder (and finally the app directory) instead of failing the whole extension.
        foreach (var candidate in new[]
                 {
                     TryResolveSettingsDirectory(),
                     Path.Combine(Path.GetTempPath(), "ItsycalForWindows"),
                     AppContext.BaseDirectory,
                 })
        {
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(candidate);
                return Path.Combine(candidate, "settings.json");
            }
            catch (Exception)
            {
            }
        }

        return Path.Combine(Path.GetTempPath(), "ItsycalForWindows", "settings.json");
    }

    private static string? TryResolveSettingsDirectory()
    {
        try
        {
            return Utilities.BaseSettingsPath("ItsycalForWindows");
        }
        catch (Exception)
        {
            return null;
        }
    }
}
