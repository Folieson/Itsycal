using System;
using System.Globalization;
using System.Timers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ItsycalForWindows.Pages;

/// <summary>
/// Dock band button: shows the current date and opens the calendar flyout when activated.
/// The text refreshes once a minute so the Dock stays correct across midnight; properties
/// only change when the formatted date actually changes.
/// </summary>
internal sealed partial class DockDateItem : ListItem, IDisposable
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    private readonly Timer _timer;
    private string? _renderedTitle;
    private string? _renderedSubtitle;
    private volatile bool _disposed;

    public DockDateItem(ICommand flyout)
        : base(flyout)
    {
        Icon = new IconInfo("\uE787"); // Segoe Fluent: Calendar
        UpdateDateText();

        // The provider wiring has no hook that disposes dock items, so the timer must not
        // keep a discarded item alive: the tick holds only a weak reference and retires the
        // timer once the item has been collected.
        var self = new WeakReference<DockDateItem>(this);
        var timer = new Timer(RefreshInterval.TotalMilliseconds) { AutoReset = true };
        _timer = timer;
        timer.Elapsed += (_, _) =>
        {
            if (self.TryGetTarget(out var item) && !item._disposed)
            {
                item.UpdateDateText();
                return;
            }

            timer.Dispose();
        };
        timer.Start();
    }

    /// <summary>
    /// Stops and releases the minute timer. The extension dispose path does not reach dock
    /// items yet (the provider owns them), so this is called only once the provider learns
    /// to dispose its dock bands; the timer also retires itself once this item is collected.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
    }

    private void UpdateDateText()
    {
        var now = DateTime.Now;
        var culture = CultureInfo.CurrentCulture;

        // The Dock button carries the glanceable date ("Sat 7 Nov"); the subtitle keeps the
        // full culture-formatted date for the secondary, tooltip-like line.
        var title = now.ToString("ddd d MMM", culture);
        var subtitle = now.ToString("D", culture);

        if (!string.Equals(title, _renderedTitle, StringComparison.Ordinal))
        {
            _renderedTitle = title;
            Title = title;
        }

        if (!string.Equals(subtitle, _renderedSubtitle, StringComparison.Ordinal))
        {
            _renderedSubtitle = subtitle;
            Subtitle = subtitle;
        }
    }
}
