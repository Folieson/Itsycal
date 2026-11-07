// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using ItsycalForWindows.Pages;
using ItsycalForWindows.Services;

namespace ItsycalForWindows;

/// <summary>
/// Root command provider: a top-level command that opens the calendar, and one dock band
/// whose button shows the current date and expands into the calendar flyout.
/// </summary>
/// <remarks>
/// The calendar page and the dock item are single cached instances. The host may query
/// the dock bands repeatedly; creating a new page each time would reset the selected
/// month, and creating a new <see cref="DockDateItem"/> each time would leave its refresh
/// timer running for an object the host no longer shows.
/// </remarks>
public partial class ItsycalForWindowsCommandsProvider : CommandProvider
{
    private const string ExtensionId = "com.folieson.itsycalforwindows";

    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();
    private readonly IEventSource _eventSource = new AppointmentsEventSource();
    private readonly CalendarPage _calendarPage;
    private readonly DockDateItem _dockItem;

    public ItsycalForWindowsCommandsProvider()
    {
        Id = ExtensionId;
        DisplayName = "Itsycal for Windows";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        _calendarPage = new CalendarPage(_settingsManager, _eventSource);
        _dockItem = new DockDateItem(_calendarPage);

        _commands =
        [
            new CommandItem(_calendarPage)
            {
                Title = DisplayName,
                Subtitle = "Month calendar and events",
                MoreCommands = [new CommandContextItem(_settingsManager.Settings.SettingsPage)],
            },
        ];

        Settings = _settingsManager.Settings;
    }

    public override ICommandItem[] TopLevelCommands() => _commands;

    /// <summary>
    /// The Dock band: one expandable button (a content page) that opens the calendar.
    /// </summary>
    public override ICommandItem[]? GetDockBands() => [_dockItem];

    /// <summary>
    /// Releases resources owned by the provider. Called from the extension's
    /// <see cref="ItsycalForWindows.Dispose"/> because the host offers no disposal callback
    /// for command providers.
    /// </summary>
    public void DisposeResources() => _dockItem.Dispose();
}
