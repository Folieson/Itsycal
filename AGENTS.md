# AGENTS.md

Project instructions for AI agents. DeepSeek Harness loads this file for every session in
this repository, after the user-global `~/.dsh/AGENTS.md`. Nested files are more specific:
`windows/AGENTS.md` governs the Windows port. System, developer, and direct user
instructions always win.

## What this repository is

`Folieson/Itsycal` is a **fork** of [sfsam/itsycal](https://github.com/sfsam/itsycal) — a
macOS menu-bar calendar written in Objective-C — plus a Windows implementation:

- **Upstream macOS app** (root, `Itsycal/`, `Itsycal.xcodeproj/`, `Design/`): reference
  implementation and the source of the product's behaviour and look. Keep it intact.
- **`windows/`** — **Itsycal for Windows**: a PowerToys Command Palette extension
  (C#/.NET 10, MSIX) that puts a calendar in the Command Palette Dock, the Windows
  equivalent of a menu-bar widget.

Both are MIT licensed; see `LICENSE.txt` and `windows/THIRD-PARTY-NOTICES.md`.

## Repository map

| Path | Purpose |
|---|---|
| `windows/` | Everything for the Windows port: solution, extension source, docs, tests. |
| `windows/AGENTS.md` | Extension-specific invariants (COM server, CLSID, dock bands, packaging). |
| `windows/src/ItsycalForWindows/` | The CmdPal extension project. |
| `windows/docs/cmdpal-extension-reference.md` | Command Palette extension API reference (from the PowerToys MIT template). |
| `.agents/skills/` | Local skills: `git-*`, `agent-notes`, and CmdPal skills (`add-dock-band`, `add-adaptive-card-form`, `add-extension-settings`, `add-fallback-commands`, `publish-extension`). |
| `.agents/agents/` | Delegation role cards: `lead`, `implementer`, `reviewer`, `researcher`, `verifier`. |
| `.agents/notes/` | Decision records: `<status>/<kind>/<YYYY-MM-DD>-<slug>.md`. |
| `.githooks/` | Conventional Commits, staged-content safety, and `main` force-push/deletion guards. |
| `scripts/scaffold-check.mjs` | Validates skills, roles, notes, AGENTS.md budget, and the CmdPal manifest/CLSID wiring. |
| `.github/workflows/windows-build.yml` | CI: Windows build and scaffold validation. |

## Hard rules

- **Windows changes stay in `windows/`.** Do not edit the macOS sources to implement
  Windows behaviour. If upstream behaviour matters, cite the exact upstream path in the
  PR or a decision record instead.
- **Do not break upstream mergeability.** Never rewrite upstream files or history; keep
  additions in new paths. Never force-push `master` or `main`.
- **Keep the manifest wiring consistent.** The extension CLSID must match in
  `ItsycalForWindows.cs` (`[Guid]`), `Package.appxmanifest` (`com:Class Id` and
  `CreateInstance ClassId`). `scripts/scaffold-check.mjs` fails on a mismatch.
- **Never change the COM hosting pattern** in `Program.cs`; extensions run out-of-process
  and the host depends on `-RegisterProcessAsComServer`.
- **MSIX identity is release-sensitive.** Changing `Identity Name`, `Publisher`, or the
  version requires re-signing and reinstalling; record the reason in a decision record.
- **No secrets, no telemetry.** Calendar data stays on the machine; the extension reads
  the local Windows appointment store and sends nothing. Never add analytics.
- **Verify before claiming.** A UI claim needs a build plus an installed-package check;
  state explicitly what was verified and what was not.
- **One writer per path.** When several agents work, split write scopes and use worktrees.

## Build and verification

Requires the .NET 10 SDK (`dotnet --version` → 10.0.x), Windows 10 2004+ and, for a
visible result, PowerToys 0.102+ with Command Palette enabled.

```powershell
# From the repository root
dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64
node scripts/scaffold-check.mjs
git config core.hooksPath .githooks   # once per clone
```

- Build only the project you changed; a clean build must exit `0`.
- The MSIX package is produced by the same build when `EnableMsixTooling` applies; see
  `windows/README.md` for installation and the PowerToys Reload step.
- Report exact commands and exit codes. "Looks fine" is not verification.

## Workflow

1. Branch from `master`: `feat/<slug>`, `fix/<slug>`, `docs/<slug>`, `chore/<slug>`.
2. Commits: Conventional Commits; the `commit-msg` hook and CI enforce the header.
3. PRs target `master`, follow `.github/pull_request_template.md` if present, and include
   verification evidence. Keep one logical change per PR.
4. Parallel writers: create a checked-out branch per writer with
   `git worktree add ..\wt-<slug> -b <branch>` from this repository; never share one
   checkout between two writers.
5. Review with `.agents/agents/reviewer.md`; merge only when CI is green.

## Delegation

Role cards are in `.agents/agents/`. Compose a delegation prompt as: role card + task +
write scope (paths) + base branch + acceptance criteria + required report format. The Lead
owns integration, git state changes, and the final answer.

Recommended split for this repository: one writer for `Models/` + `Pages/`, one for
`Services/` + manifest capabilities, the Lead for `Program.cs`, provider wiring, packaging
and docs.

## Decision records and skills

- Cross-cutting or non-obvious choices get a note in `.agents/notes/` (path format above,
  template in `.agents/notes/TEMPLATE.md`, procedure in `.agents/skills/agent-notes/`).
- Use the local skills before inventing procedures: git work, decision records, and
  Command Palette patterns (`add-dock-band` for Dock widgets, `add-adaptive-card-form` for
  card UI, `add-extension-settings` for settings).
- Keep `AGENTS.md` under the 65,536-byte instruction budget; files under 16 KB are the
  practical target.

## Project facts

- Upstream: `sfsam/itsycal` (Objective-C, MIT); fork: `Folieson/Itsycal` (public).
- Windows product name: **Itsycal for Windows**.
- Extension identity: `ItsycalForWindows`, CLSID `D13947EE-B1EE-461A-8160-EDCAAA65A3AA`.
- Target framework: `net10.0-windows10.0.26100.0`; platform `x64` (ARM64 kept in config).
- Command Palette SDK: `Microsoft.CommandPalette.Extensions` 0.11.x (PowerToys 0.102).
- CI: `.github/workflows/windows-build.yml` (windows-latest, .NET 10, build + scaffold
  check). Every run uploads the signed `itsycal-for-windows-dev` artifact (MSIX + `.cer`).
- Install: elevated `scripts/deploy-local.ps1 -MsixPath <msix> -CertPath <cer>`; the script
  trusts the development certificate machine-wide and restarts Command Palette.
- Commit identity: repository-local `user.name` / `user.email` (GitHub noreply).
