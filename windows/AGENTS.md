# Itsycal for Windows — extension invariants

Scope: everything under `windows/`. Read the repository `AGENTS.md` first; this file adds
rules specific to the Command Palette extension.

## What this is

Itsycal for Windows is a PowerToys **Command Palette extension**. It contributes a **dock
band** (the persistent toolbar at the screen edge, the Windows analogue of the macOS menu
bar) showing the current date, and a rich **flyout** with a month calendar and events. The
user experience should feel like ItsyCal: glanceable date, one click to a month grid, no
window management.

## Non-negotiable wiring

- Extensions run **out-of-process** as MSIX-packaged COM servers. `Program.cs` hosts the
  server and must keep the `-RegisterProcessAsComServer` branch unchanged.
- The CLSID must be identical in three places: `[Guid]` in the `IExtension`
  implementation class, `com:Class Id` in `Package.appxmanifest`, and
  `CreateInstance ClassId` in the `uap3:AppExtension` block.
- The `uap3:AppExtension` must keep `Name="com.microsoft.commandpalette"` and the
  `CmdPalProvider` activation block.
- `CommandProvider.Id` must be a stable unique id; dock bands require non-empty command
  ids or the host silently ignores the item.

## Dock band rules

- `GetDockBands()` returns `ICommandItem[]`; a `ContentPage` renders as one expandable
  button with a flyout — use that for the calendar.
- Dock bands update frequently. Keep work cheap: no I/O in property getters, cache the
  month, update the date with a timer at most once per minute, dispose timers with the
  extension.
- The dock item must degrade gracefully when the flyout is closed and when no events exist.

## UI rules

- Prefer `IListPage` for searchable lists and `IContentPage` for rich content; Adaptive
  Cards (`FormContent`) render the month grid and carry month navigation.
- Navigation between months must not lose the current selection; "Today" always returns
  to the current month.
- Month grid: 6 rows × 7 columns, leading/trailing days dimmed, today highlighted, week
  start configurable (Monday/Sunday), optional ISO week numbers.
- Keep text short and localizable; no hard-coded colors beyond theme-neutral ones.

## Events

- Read events from the local Windows appointment store via `Windows.ApplicationModel.
  Appointments` (read-only). No network calls, no telemetry, no writing to the user's
  calendar.
- The package manifest declares the `appointments` capability. If the capability or API is
  unavailable at runtime, fall back to an empty event list — the calendar must never fail
  because events are unavailable.
- Events are cached per visible month range; refresh when the month changes or the flyout
  opens, not on every render tick.
- Distinguish all-day events and multi-day spans explicitly in the model.

## Build, package, deploy

```powershell
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
```

- The build produces the extension binaries and an MSIX layout; installation requires a
  trusted signing certificate (or Developer Mode for a dev package) — see
  `windows/README.md`.
- After installing or rebuilding, restart the extension: Command Palette → `Reload` →
  "Reload Command Palette extensions", or restart PowerToys.
- Never claim the UI works from a successful build alone; a build plus an installed and
  reloaded package is the minimum evidence.

## Anti-patterns

- Creating a new extension instance per host callback (the `Program.cs` pattern
  deliberately keeps one instance).
- Logging or file I/O inside `GetItems()` / dock property getters.
- Adding NuGet packages without recording them in `windows/THIRD-PARTY-NOTICES.md`.
- Editing the upstream macOS sources to change Windows behaviour.
- Committing `bin/`, `obj/`, `AppPackages/`, certificates, or generated manifests.
