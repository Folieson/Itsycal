# AGENTS.md

Contributor rules for AI coding agents working in this repository. This is the Windows-only
repository of **Itsycal for Windows**, a PowerToys Command Palette extension; it is derived
from the MIT-licensed upstream [ItsyCal](https://github.com/sfsam/itsycal) macOS project,
which is not part of this repository anymore.

## Layout

- `windows/` — the Windows extension: C#/.NET 10, Command Palette dock band + calendar.
- `windows/src/ItsycalForWindows/` — extension source (`Program.cs`, provider, `Models/`,
  `Pages/`, `Services/`, package assets).
- `windows/tests/` — unit tests for the pure logic (calendar grid, event ranges).
- `windows/scripts/` — development tooling: `deploy-local.ps1` (MSIX dev install),
  `Build-Release.ps1` (signed release assets), `New-BrandAssets.ps1`.
- `winget/` — WinGet manifests for the MSIX release (`InstallerType: msix`).
- `docs/` — architecture decision records and `docs/SIGNING.md`.

## Hard rules

- All behaviour changes go in `windows/`. Do not reintroduce macOS sources; refer to
  upstream by link when behaviour parity matters.
- The extension is an out-of-process COM server. Keep `Program.cs`'s
  `-RegisterProcessAsComServer` branch intact; the CLSID must stay identical in
  `ItsycalForWindows.cs` and `Package.appxmanifest` (`com:Class Id`, `CreateInstance
  ClassId`).
- Never commit `bin/`, `obj/`, generated packages, certificates, `*.msix`, `publish/`, or
  `windows/artifacts/` output.
- Do not add telemetry or network calls. Calendar data never leaves the machine; do not log
  event titles.
- Ask before changing the MSIX identity (`Name`, `Publisher`, version), the WinGet package
  identifier, or the signing story — all are release-sensitive.
- Verify before reporting: a build/`dotnet test` exit code is evidence; "should work" is
  not. For UI claims, state what was actually observed.

## Build and verify

```powershell
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
dotnet test windows/tests/ItsycalForWindows.Tests.csproj -c Debug
```

Public releases are signed MSIX packages built per architecture by
`windows/scripts/Build-Release.ps1`. Command Palette discovers extensions only through the
MSIX app-extension catalog, so there is no unpackaged build — see `windows/README.md` and
`docs/SIGNING.md`. The release pipeline is `.github/workflows/release.yml` (tag `v*`); CI
builds, packages and tests every PR.

## Commits and PRs

Conventional Commits (`feat(windows): ...`, `fix(windows): ...`, `docs: ...`), one logical
change per PR, PR description with the exact verification commands and results. Keep
`docs/adr/` up to date when a decision changes.

## Decision records

Architecture decisions live in `docs/adr/` (`NNNN-kebab-title.md`).
