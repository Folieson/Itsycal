using System;
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

    private readonly ToggleSetting _weekStartsOnMonday = new(
        Namespaced("WeekStartsOnMonday"),
        "Week starts on Monday",
        "Use Monday as the first day of the week in the calendar grid.",
        CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek == DayOfWeek.Monday);

    private readonly ToggleSetting _showWeekNumbers = new(
        Namespaced("ShowWeekNumbers"),
        "Show ISO week numbers",
        "Show a week-number column in the month grid.",
        false);

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        Settings.Add(_weekStartsOnMonday);
        Settings.Add(_showWeekNumbers);

        // Load settings from file upon initialization.
        LoadSettings();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    /// <summary>First day of the week used by the calendar grid.</summary>
    public DayOfWeek WeekStart => _weekStartsOnMonday.Value ? DayOfWeek.Monday : DayOfWeek.Sunday;

    /// <summary>True when the grid should show ISO week numbers.</summary>
    public bool ShowWeekNumbers => _showWeekNumbers.Value;

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("ItsycalForWindows");
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }
}
