# Port ItsyCal to Windows as a Command Palette dock-band extension

Status: implemented
Date: 2026-11-07

## Context

ItsyCal is a macOS menu-bar calendar in Objective-C: one glanceable date in the menu bar,
one click to a month grid, events from the system calendar. Windows has no menu bar
equivalent, and the fork must stay buildable for macOS, so the Windows work cannot be a
port of the Objective-C sources.

PowerToys 0.102 ships **Command Palette**, and its **Dock** is a persistent toolbar at the
screen edge — the closest Windows surface to a menu bar widget. Its extension SDK
(`Microsoft.CommandPalette.Extensions` 0.11) supports contributing dock bands and rich
flyouts. The machine already runs PowerToys 0.102.

## Decision

1. **Keep upstream untouched**; add `windows/` to the fork. The macOS app remains the
   reference for behaviour and look, but no upstream file is edited.
2. **Implement the Windows product as a Command Palette extension** (C#, .NET 10, MSIX),
   not a standalone tray application: the Dock provides the always-visible top-of-screen
   placement the user asked for, plus theming, input, and distribution for free.
3. **One dock band + one content page.** The band shows the current date and refreshes once
   a minute; the page renders a 42-cell month grid (today highlighted, month navigation)
   and the visible range's events.
4. **Events are read-only** from the local Windows appointment store
   (`Windows.ApplicationModel.Appointments`, `appointments` capability). No network, no
   telemetry; any access failure degrades to an event-free calendar.
5. **Adopt the agentic scaffold** from the starter kit (root `AGENTS.md`, `.agents/`,
   `.githooks/`, `scripts/scaffold-check.mjs`) so forks of this shape are developed by
   agents under the same rules, with a Windows CI job as the build gate.

## Alternatives considered

- **Standalone tray application** — rejected: no always-visible top-of-screen band, and it
  would duplicate settings, theming, and installer work the PowerToys host already does.
- **Win32 topmost floating bar** — rejected: looks like a second taskbar, breaks on
  multi-monitor/DPI changes, and needs its own autostart and update channel.
- **Port the Objective-C code** — rejected: EventKit and AppKit are macOS-only; the result
  would be a rewrite anyway, and it would touch upstream files.
- **Full ItsyCal feature parity in the first cut** — rejected for sequencing: event
  create/edit requires write access and conflict handling; the read-only MVP proves the
  integration without becoming a calendar client.

## Consequences

- Positive: one extension delivers date glanceability, a month grid, and events with no
  extra windows; upstream stays mergeable; CI on `windows-latest` builds every PR because
  the local machine initially lacked the Windows SDK.
- Negative: the Dock's placement and visibility are PowerToys settings, not ours; a
  packaged identity (MSIX) is required for events, so an unpackaged debug run shows none;
  the PowerToys extension API is young and may change.
- Neutral: the macOS app and the Windows extension share branding and intent, not code.

## Follow-ups / deferred work

- [ ] Automate local MSIX signing/install (`scripts/deploy-local.ps1`) and document it.
- [ ] Add unit tests for the month-grid arithmetic and the event-range mapping.
- [ ] Day selection and per-day event list; keyboard navigation inside the flyout.
- [ ] Event alerts/reminders (ItsyCal parity), which need write access and are out of scope
      for the read-only first cut.
- [ ] Publishing path (Store or WinGet) per the CmdPal `publish-extension` skill.

## References

- `AGENTS.md`, `windows/AGENTS.md` — rules this decision established.
- `windows/docs/cmdpal-extension-reference.md` — extension API reference (MIT, PowerToys).
- `.agents/skills/add-dock-band/SKILL.md` — dock-band contract.
- `.agents/skills/add-adaptive-card-form/SKILL.md` — card UI used by the month grid.
- `Itsycal/` — upstream implementation this product mirrors in spirit.
