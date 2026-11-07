# Itsycal for Windows

A calendar for the **Windows PowerToys Command Palette Dock** — the closest Windows
equivalent of [ItsyCal](https://github.com/sfsam/itsycal)'s menu-bar calendar.

This repository is a fork of [sfsam/itsycal](https://github.com/sfsam/itsycal). The macOS
application is upstream and untouched; the Windows product lives in `windows/`.

## Features

- **Date in the Dock** — an always-visible band at the screen edge showing today's date;
  refreshes automatically across midnight.
- **Month calendar** — one click opens a flyout with a 6×7 month grid, today highlighted,
  adjacent-month days dimmed, and previous/next/today navigation.
- **ISO week numbers** — optional week-number column (ISO-8601).
- **Events** — read-only events from the local Windows calendar for the visible range, with
  all-day and multi-day handling; the calendar keeps working when no calendar is configured.
- **Settings** — first day of the week and week-number visibility.
- **Private by design** — no network calls, no telemetry, calendar data never leaves the
  machine.

## Requirements

| Requirement | Version |
|---|---|
| Windows | 10 2004 (build 19041) or newer |
| PowerToys | 0.102 or newer, with Command Palette enabled |
| Runtime | .NET 10 Desktop Runtime (installed by the installer when missing) |

## Install

```powershell
winget install Folieson.ItsycalForWindows
```

Until the WinGet manifest is accepted, install the signed installer from the
[latest release](https://github.com/Folieson/Itsycal/releases/latest):

```powershell
# after downloading ItsycalForWindows_x.y.z_x64.exe
.\ItsycalForWindows_x.y.z_x64.exe /VERYSILENT /SUPPRESSMSGBOXES
```

Then open **Command Palette → Dock settings** and enable the **Itsycal for Windows** band.

## Build from source

Requires the .NET 10 SDK and the Windows SDK; MSIX packaging additionally needs the Windows
Application Packaging tooling from Visual Studio.

```powershell
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
dotnet test  windows/tests/ItsycalForWindows.Tests.csproj -c Debug
```

Development install (MSIX, elevated shell) and unpackaged release packaging are described in
[windows/README.md](windows/README.md).

## Configuration

The extension shows a settings page inside Command Palette: open the extension's commands
and choose **Settings** to switch the first day of the week or show ISO week numbers.
Settings are stored by Command Palette.

## Privacy

The extension reads events through the read-only Windows appointment store
(`Windows.ApplicationModel.Appointments`). It never writes to the calendar, never sends data
anywhere, and logs no event content. Access is declared with the `appointmentsSystem`
capability in the package manifest.

## Repository layout

| Path | Purpose |
|---|---|
| `windows/` | Windows extension source, tests, build scripts. |
| `windows/src/ItsycalForWindows/` | The Command Palette extension (C#, .NET 10). |
| `windows/tests/` | Unit tests for the pure calendar/event logic. |
| `installer/` | Inno Setup script used for WinGet releases. |
| `docs/adr/` | Architecture decision records. |
| `Itsycal/`, `Itsycal.xcodeproj/`, … | Upstream macOS application (unchanged). |

## Contributing

Contributions are welcome. Read [AGENTS.md](AGENTS.md) for repository rules (they apply to
human and AI contributors alike), then open a pull request using the template. Please keep
Windows changes inside `windows/` and leave the macOS sources untouched.

## License

MIT, same as upstream ItsyCal. See [LICENSE.txt](LICENSE.txt). The Windows port derives its
extension skeleton from the Microsoft PowerToys template; see
[windows/THIRD-PARTY-NOTICES.md](windows/THIRD-PARTY-NOTICES.md).
