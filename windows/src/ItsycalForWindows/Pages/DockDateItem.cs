using System;
using System.Globalization;
using System.Timers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ItsycalForWindows.Pages;

/// <summary>
/// Dock band button: shows the current date and opens the calendar flyout when activated.
/// The title refreshes once a minute so the Dock stays correct across midnight.
/// </summary>
internal sealed partial class DockDateItem : ListItem
{
    private readonly Timer _timer;

    public DockDateItem(ICommand flyout)
        : base(flyout)
    {
        Icon = new IconInfo("\uE787"); // Segoe Fluent: Calendar
        UpdateTitle();

        _timer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds) { AutoReset = true };
        _timer.Elapsed += (_, _) => UpdateTitle();
        _timer.Start();
    }

    private void UpdateTitle()
    {
        Title = DateTime.Now.ToString("ddd d MMM", CultureInfo.CurrentCulture);
        Subtitle = DateTime.Now.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
    }
}
