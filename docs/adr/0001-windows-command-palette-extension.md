# ADR 0001 — Ship Itsycal for Windows as a Command Palette dock-band extension

- Status: accepted
- Date: 2026-11-07
- Deciders: project maintainer

## Context

ItsyCal is a macOS menu-bar calendar: a glanceable date in the menu bar, one click to a
month grid, and events from the system calendar. The fork must keep the macOS application
buildable, so the Windows product cannot be a port of the Objective-C sources.

PowerToys 0.102 ships **Command Palette**, whose **Dock** is a persistent toolbar at the
screen edge — the closest Windows surface to a menu bar widget. Its extension SDK
(`Microsoft.CommandPalette.Extensions`, 0.11) supports contributing dock bands and rich
flyouts.

## Decision

1. Add `windows/` to the fork; leave the macOS sources untouched.
2. Implement the Windows product as a Command Palette extension (C#, .NET 10), not a
   standalone tray application: the Dock gives the always-visible placement, plus theming,
   input, and discovery through PowerToys.
3. One dock band shows the current date (refreshed once a minute) and opens a month flyout:
   42-cell grid, ISO week numbers, configurable week start, today highlighted, month
   navigation, and events for the visible range.
4. Events are read-only from the local Windows appointment store
   (`Windows.ApplicationModel.Appointments`, restricted capability `appointmentsSystem`).
   No network, no telemetry; any access failure degrades to a calendar without events.
5. Distribution:
   - **WinGet / public releases:** unpackaged publish (`WindowsPackageType=None`) wrapped
     in an Inno Setup installer, registered as a per-user COM server in `HKCU`; no
     code-signing certificate is required for `winget install`.
   - **Development:** MSIX package for local install via `windows/scripts/deploy-local.ps1`.
6. The extension ships no autonomous updater; WinGet/`winget upgrade` owns updates, and the
   repository tags releases (`v*`) whose workflow builds the installers and attaches them.

## Alternatives considered

- **Standalone tray application** — no always-visible top-of-screen band, and it would
  duplicate settings, theming, and update distribution.
- **Win32 topmost floating bar** — looks like a second taskbar, breaks on multi-monitor and
  DPI changes, and needs its own autostart and update channel.
- **Port the Objective-C code** — EventKit/AppKit are macOS-only; the result would be a
  rewrite that also touches upstream files.
- **Writing and editing events in the first release** — requires write access and conflict
  handling; the read-only integration proves the product without becoming a calendar
  client first.

## Consequences

- Positive: one extension delivers date glanceability, a month grid, and events with no
  extra windows; upstream stays mergeable; WinGet distribution needs no certificate.
- Negative: Dock placement and visibility are PowerToys settings, not ours; the CmdPal
  extension API is young; the MSIX identity and the WinGet package identifier must stay in
  sync with the installer's COM registration.
- Neutral: the macOS app and the Windows extension share branding and intent, not code.

## Follow-ups

- [ ] Unit tests for the calendar grid, ISO weeks, day spans, and event ranges (in
      progress).
- [ ] Day selection with per-day event details and "open in Calendar".
- [ ] WinGet manifest submission (`Folieson.ItsycalForWindows`) after the first tagged
      release.
- [ ] Localization beyond English.
- [ ] Event reminders/alerts (ItsyCal parity); needs write access, out of scope for now.
