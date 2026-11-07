## What and why

<!-- One paragraph. Link the issue if there is one. -->

## How verified

<!-- Exact commands and results, not "it builds". -->
- `dotnet build windows/ItsycalForWindows.sln -c Debug -p:Platform=x64` — exit code:
- `dotnet test windows/tests/ItsycalForWindows.Tests.csproj -c Debug` — exit code:
- Other checks (installer, Command Palette behaviour):

## Risk and rollback

- Risk:
- Rollback:

## Checklist

- [ ] Windows changes stay in `windows/`; upstream macOS sources untouched.
- [ ] CLSID is unchanged in `ItsycalForWindows.cs` and `Package.appxmanifest`.
- [ ] No telemetry or network code; event content is not logged.
- [ ] New NuGet packages are recorded in `windows/THIRD-PARTY-NOTICES.md`.
- [ ] No `bin/`, `obj/`, `publish/`, certificates or packages in the diff.
- [ ] README/ADR updated when behaviour or a decision changed.
