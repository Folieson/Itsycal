# AGENTS.md

Contributor rules for AI coding agents working in this repository. This repository is a
fork of [ItsyCal](https://github.com/sfsam/itsycal) that adds **Itsycal for Windows**, a
PowerToys Command Palette extension. A subset of the rules applies to the upstream macOS
sources, which are kept intact.

## Layout

- `windows/` — the Windows extension: C#/.NET 10, Command Palette dock band + calendar.
- `windows/src/ItsycalForWindows/` — extension source (`Program.cs`, provider, `Models/`,
  `Pages/`, `Services/`, package assets).
- `windows/tests/` — unit tests for the pure logic (calendar grid, event ranges).
- `windows/scripts/` — development tooling, including `Build-Release.ps1` (signed MSIX
  release assets).
- `winget/` — WinGet manifests for the MSIX release (`InstallerType: msix`).
- `docs/` — architecture decision records and the signing/distribution guide
  (`docs/SIGNING.md`).
- Everything else at the repository root is upstream ItsyCal (Objective-C, macOS).

## Hard rules

- Windows behaviour changes go in `windows/`. Do not modify the macOS sources; keep the
  fork mergeable with upstream.
- The extension is an out-of-process COM server. Keep `Program.cs`'s
  `-RegisterProcessAsComServer` branch intact; the CLSID must stay identical in
  `ItsycalForWindows.cs` and `Package.appxmanifest` (`com:Class Id`, `CreateInstance
  ClassId`).
- Never commit `bin/`, `obj/`, generated packages, certificates, `*.msix`, or
  `windows/artifacts/` output.
- Do not add telemetry or network calls. Calendar data never leaves the machine; do not log
  event titles.
- Ask before changing the MSIX identity (`Name`, `Publisher`, version) or the WinGet
  package identifier — both are release-sensitive.
- Verify before reporting: a build/`dotnet test` exit code is evidence; "should work" is
  not. For UI claims, state what was actually observed.

## Build and verify

```powershell
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
dotnet test windows/tests/ItsycalForWindows.Tests.csproj -c Debug
```

- The default build is the MSIX development package. Public releases are signed MSIX
  packages built per architecture by `windows/scripts/Build-Release.ps1`; Command Palette
  discovers extensions only through the MSIX app-extension catalog, so there is no
  unpackaged build — see `windows/README.md` and `docs/SIGNING.md`.
- The release pipeline is `.github/workflows/release.yml` (tag `v*`); CI builds and tests
  every PR. A WinGet submission requires a trusted code-signing certificate; the staged
  manifests live in `winget/`.

## Commits and PRs

Conventional Commits (`feat(windows): ...`, `fix(windows): ...`, `docs: ...`), one logical
change per PR, PR description with the exact verification commands and results. Keep
`docs/adr/` up to date when a decision changes.

## Decision records

Architecture decisions live in `docs/adr/` (`NNNN-kebab-title.md`). This repository
previously used an agent-owned note directory; decisions were moved to ADR format.
