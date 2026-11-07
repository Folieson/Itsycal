# Itsycal for Windows

A calendar for the Windows **PowerToys Command Palette Dock** — the closest Windows
equivalent of [ItsyCal](https://github.com/sfsam/itsycal)'s menu-bar calendar. This is the
Windows half of the `Folieson/Itsycal` fork; the upstream macOS application is untouched.

## What it does

- Adds a dock band to the PowerToys Command Palette Dock showing today's date.
- Opens a flyout with a month grid: current month, today highlighted, previous/next month
  and "Today".
- Shows the day's events from the local Windows calendar (read-only), with an explicit
  empty state when no calendar is configured or accessible.
- Stays out of the way: no windows, no tray icon, no network access, no telemetry.

The goal is feature parity in spirit, not a line-by-line port: ItsyCal is Objective-C and
macOS-only, so the Windows implementation is a new C# extension built on the
[Command Palette extension SDK](https://learn.microsoft.com/windows/powertoys/command-palette/extension-development).

## Requirements

| Requirement | Version |
|---|---|
| Windows | 10 2004 (build 19041) or newer |
| PowerToys | 0.102 or newer, with Command Palette enabled |
| .NET SDK (build only) | 10.0.x |
| Platform | `x64` (ARM64 project configuration exists but is unverified) |

## Build

```powershell
# From the repository root
dotnet restore windows/ItsycalForWindows.sln
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
```

Output lands under `windows/src/ItsycalForWindows/bin/x64/Debug/`.

## Install for development

The extension is a packaged COM server, so a plain build is not enough — the MSIX package
must be registered:

1. Enable **Developer Mode** (Settings → System → For developers) or trust the package's
   signing certificate.
2. Build the package: `dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64`
   and locate the generated `.msix` under the project output (`AppPackages`).
3. Install it: `Add-AppxPackage -Path <path-to.msix>`.
4. Restart Command Palette, or run `Reload` → "Reload Command Palette extensions".
5. Open the Dock settings and enable the **Itsycal for Windows** band.

Automated local deployment is still being finalised (the PowerToys template normally
deploys from Visual Studio); a CLI script will be added under `scripts/` once verified.

## Events and privacy

Events come from the Windows appointment store through the read-only
`Windows.ApplicationModel.Appointments` API. The extension never writes to the calendar,
never sends data anywhere, and degrades to a calendar without events when access is
unavailable. No analytics or logging of event contents exists.

## Limitations

- Events require the packaged identity to declare the `appointments` capability; an
  unpackaged debug run shows no events.
- The Dock layout (position, pinning) is configured in PowerToys, not by this extension.
- Localization exists only for English so far.

## Layout

```
windows/
  ItsycalForWindows.sln
  Directory.Build.props / Directory.Packages.props / nuget.config
  docs/cmdpal-extension-reference.md      # API reference from the PowerToys MIT template
  src/ItsycalForWindows/
    Program.cs                            # COM server host
    ItsycalForWindows.cs                  # IExtension implementation (CLSID)
    ItsycalForWindowsCommandsProvider.cs  # commands + dock bands
    Models/                               # month grid, days, events
    Pages/                                # calendar flyout pages
    Services/                             # appointment store access
    Assets/                               # package logos
    Package.appxmanifest                  # identity, COM server, CmdPal app extension
```

## License

MIT, like upstream ItsyCal. See `../LICENSE.txt` and `THIRD-PARTY-NOTICES.md`.
