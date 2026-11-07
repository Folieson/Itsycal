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
public partial class ItsycalForWindowsCommandsProvider : CommandProvider
{
    private const string ExtensionId = "com.folieson.itsycalforwindows";

    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();
    private readonly IEventSource _eventSource = new AppointmentsEventSource();

    public ItsycalForWindowsCommandsProvider()
    {
        Id = ExtensionId;
        DisplayName = "Itsycal for Windows";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        _commands =
        [
            new CommandItem(new CalendarPage(_settingsManager, _eventSource))
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
    public override ICommandItem[]? GetDockBands() =>
    [
        new DockDateItem(new CalendarPage(_settingsManager, _eventSource)),
    ];
}
