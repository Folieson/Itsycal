# Itsycal for Windows — extension source

This folder contains the Windows product: a PowerToys Command Palette extension written in
C#/.NET 10. It contributes a dock band with the current date and a flyout month calendar
with read-only events from the Windows appointment store.

## Prerequisites

| Tool | Purpose |
|---|---|
| .NET 10 SDK (10.0.x) | build, tests, publish |
| Windows SDK 10.0.26100 | CsWinRT projection and MSIX packaging |
| PowerToys 0.102+ | Command Palette host |
| Inno Setup 6 (release only) | WinGet installer packaging |

## Build and test

```powershell
# from the repository root
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
dotnet test  windows/tests/ItsycalForWindows.Tests.csproj -c Debug
```

The default configuration builds the **MSIX development package** (useful for local
installation and debugging). The WinGet release builds **unpackaged**:

```powershell
dotnet publish windows/src/ItsycalForWindows/ItsycalForWindows.csproj `
  -c Release -r win-x64 --self-contained false `
  -p:WindowsPackageType=None -p:EnableMsixTooling=false -o publish/x64
```

`installer/setup.iss` wraps that output into `ItsycalForWindows_<version>_<arch>.exe` and
registers the COM server under `HKCU`, which is what the WinGet manifest points at.

## Install a development build (MSIX)

MSIX sideloading trust is machine-wide, so the development installer must run elevated:

```powershell
# 1. download the artifact from the latest CI run
gh run download <run-id> -n itsycal-for-windows-dev -D .\pkg

# 2. install (elevated PowerShell)
.\windows\scripts\deploy-local.ps1 -MsixPath .\pkg\ItsycalForWindows_*.msix -CertPath .\pkg\devcert.cer
```

The development certificate is ephemeral (one per CI run); re-trust the matching `.cer` when
you install a package from a different run.

## Source map

| Path | Purpose |
|---|---|
| `src/ItsycalForWindows/Program.cs` | Out-of-process COM server host. Do not change the hosting pattern. |
| `src/ItsycalForWindows/ItsycalForWindows.cs` | `IExtension` implementation; carries the CLSID. |
| `src/ItsycalForWindows/ItsycalForWindowsCommandsProvider.cs` | Commands, dock band, cached page instance. |
| `src/ItsycalForWindows/Models/` | Calendar month grid, week rows, day/event models (pure logic). |
| `src/ItsycalForWindows/Pages/` | Dock item and Adaptive Card calendar flyout. |
| `src/ItsycalForWindows/Services/` | Appointment-store access and event range helpers. |
| `tests/` | Unit tests for the pure logic. |
| `scripts/` | Development tooling (MSIX install script, icon generation). |
| `Package.appxmanifest` | MSIX identity, COM server, CmdPal app extension, `appointmentsSystem` capability. |

## Events and privacy

Events come from `Windows.ApplicationModel.Appointments` in read-only mode
(`AppointmentStoreAccessType.AllCalendarsReadOnly`). The extension never writes to the
calendar, makes no network calls, and logs no event content. Without the capability, a
packaged run simply shows no events; the calendar still works.

## Troubleshooting

- **Band not visible** — enable it in Command Palette → Dock settings; the host persists the
  selection per user.
- **Extension not listed** — restart Command Palette (or `Reload` → “Reload Command Palette
  extensions”); for unpackaged installs check `HKCU\Software\Classes\CLSID\{D13947EE-B1EE-461A-8160-EDCAAA65A3AA}`.
- **No events** — the appointment store is empty, no calendar account is configured, or the
  app is running unpackaged without the `appointmentsSystem` capability (the last case is
  expected for a plain `dotnet run`).
