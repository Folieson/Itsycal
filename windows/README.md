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
must be registered with a certificate the machine trusts.

The supported path on a machine without the Windows SDK (the normal case when the package
is built in CI):

1. Take the `itsycal-for-windows-dev` artifact from the latest successful
   `windows-build` run (`gh run download <run-id> -n itsycal-for-windows-dev -D .\pkg`).
   It contains a signed `ItsycalForWindows_*.msix` and the matching `devcert.cer`.
2. Run the installer from an **elevated** PowerShell:

   ```powershell
   .\scripts\deploy-local.ps1 -MsixPath .\pkg\ItsycalForWindows_*.msix -CertPath .\pkg\devcert.cer
   ```

   The script trusts the development certificate in `LocalMachine\TrustedPeople`, registers
   the package, and restarts Command Palette.
3. Enable the **Itsycal for Windows** band in the Command Palette Dock settings.

Building and packaging locally (needs the Windows SDK and .NET 10 SDK):

```powershell
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64 -p:GenerateAppxPackageOnBuild=true
Add-AppxPackage -Path <generated .msix>   # after trusting the signing certificate
```

The development certificate is ephemeral (one per CI run), so a package from an older run
must be re-trusted with its own `.cer`.

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
