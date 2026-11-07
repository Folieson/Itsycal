# Third-party notices

This repository is a fork of [ItsyCal](https://github.com/sfsam/itsycal) by Sam Vermette
and contributors, licensed under the MIT License. The upstream sources are unmodified.

The Windows port reuses material from Microsoft PowerToys:

- `windows/docs/cmdpal-extension-reference.md` and the CmdPal skills under
  `.agents/skills/add-dock-band/`, `.agents/skills/add-adaptive-card-form/`,
  `.agents/skills/add-extension-settings/`, `.agents/skills/add-fallback-commands/`, and
  `.agents/skills/publish-extension/` are derived from
  [microsoft/PowerToys](https://github.com/microsoft/PowerToys), MIT License,
  Copyright (c) Microsoft Corporation.
- The extension project skeleton (`Program.cs`, `app.manifest`,
  `Package.appxmanifest`, project files, package assets) is derived from the
  `TemplateCmdPalExtension` in the same repository, MIT License.

No other third-party code is vendored. NuGet dependencies are declared centrally in
`windows/Directory.Packages.props`; add new packages there and record them here.
