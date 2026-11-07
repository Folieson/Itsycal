## What and why

<!-- One paragraph: the problem, the approach, the outcome. Link the issue. -->
Closes #

## How verified

<!-- Exact commands and results, not "it builds". -->
- `dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64` — exit code:
- `node scripts/scaffold-check.mjs` — exit code:
- Command Palette behaviour checked after `Add-AppxPackage` + `Reload`? (yes/no, details)

## Risk and rollback

- Risk:
- Rollback:

## Notes

- Decision record:
- Follow-ups:

## Checklist

- [ ] Windows changes stay in `windows/`; upstream macOS sources untouched.
- [ ] CLSID matches in `ItsycalForWindows.cs`, `com:Class Id`, and `CreateInstance ClassId`.
- [ ] `Program.cs` COM hosting pattern unchanged.
- [ ] New NuGet packages recorded in `windows/THIRD-PARTY-NOTICES.md`.
- [ ] No secrets, certificates, `bin/`, `obj/`, or `AppPackages/` in the diff.
- [ ] CI `windows-build` is green.
