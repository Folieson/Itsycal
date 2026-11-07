# Contributing to Itsycal for Windows

Thanks for taking the time to contribute. This repository is the Windows-only home of the
PowerToys Command Palette extension; the original macOS ItsyCal lives
[upstream](https://github.com/sfsam/itsycal).

## Prerequisites

- Windows 10 2004+ and PowerToys 0.102+ with Command Palette enabled (to run the extension).
- .NET 10 SDK, Windows SDK 10.0.26100, and Visual Studio Build Tools with MSIX packaging (to
  build and package).
- PowerShell 7 for the scripts in `windows/scripts/`.

## Build and test

```powershell
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
dotnet test  windows/tests/ItsycalForWindows.Tests.csproj -c Debug
```

The default build is the MSIX development package. Public releases are signed MSIX packages
produced by `windows/scripts/Build-Release.ps1`; see [windows/README.md](windows/README.md)
for local installation and [docs/SIGNING.md](docs/SIGNING.md) for signing.

## Pull requests

- One logical change per PR; use the pull request template and include the exact commands
  you ran plus their results.
- Follow [AGENTS.md](AGENTS.md) — the same rules apply to human and AI contributors.
- Do not change the package identity (`Name`, `Publisher`, version), the CLSID, or the WinGet
  package identifier without discussing it first; those are release-sensitive.
- No telemetry, no network calls, and never log event content.
- Update `docs/adr/` when your change alters a documented decision.

## Reporting issues

Open a GitHub issue with the PowerToys and Windows versions, what you expected, what happened,
and the relevant Command Palette log lines (Command Palette → Settings → logging, or
`%LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\Logs`). Never paste event titles or other personal
calendar data.

## License

By contributing you agree that your contribution is licensed under the MIT License in
[LICENSE.txt](LICENSE.txt).
