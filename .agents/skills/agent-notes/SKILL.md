---
name: agent-notes
description: Use when a decision needs a durable record — choose the note path, copy the template, capture alternatives and consequences, and move it through proposed to implemented to archived. Notes are context, not spec.
---

# Decision records

Write a decision record so the reasoning behind a non-obvious choice survives the session that made it.

## Use this when
- A cross-cutting decision affects more than one module, package, or teammate.
- You rejected a credible alternative that a future reader would otherwise re-propose.
- The tradeoff is non-obvious: the chosen path looks wrong without the reason.
- An incident, a bug fix, or a process change sets a precedent.
- Not for a routine change that follows an existing pattern, or a rule already stated in a spec or AGENTS.md; link that instead of restating it.

## Procedure
1. Search existing notes first: `git grep -n -i "<topic phrase>" -- .agents/notes`, and read [notes README](../../notes/README.md) for the index, statuses, and kinds. Check all three status directories before writing.
2. Check that a note is warranted using the Decide table. If an existing note covers it, update or link that note instead of opening a duplicate.
3. Choose the status and kind, then derive the filename. Path: `.agents/notes/<status>/<kind>/<YYYY-MM-DD>-<kebab-slug>.md`. Status is `proposed`, `implemented`, or `archived`; kind is `architecture`, `feature`, `bug-fix`, `simplification`, `process`, or `testing`.
4. Copy the template: `cp .agents/notes/TEMPLATE.md .agents/notes/proposed/architecture/2026-11-07-single-writer-per-path.md` (PowerShell: `Copy-Item`). Never start from a blank file, so every heading is answered.
5. Fill every heading of [TEMPLATE.md](../../notes/TEMPLATE.md): Context (the forcing problem, constraints, prior behavior, linked files), Decision (the choice in present tense with its boundary or owner), Alternatives considered (each rejected option and why, including doing nothing), Consequences (positive, negative, neutral), Follow-ups / deferred work (owner and trigger), References (code, spec, PR, superseded record).
6. Derive the filename from the title: lowercase, replace every run of non-alphanumeric characters with one hyphen, trim leading and trailing hyphens, and keep three to six words unique in the repository. The date is the decision date in UTC, not the draft date. Example: title `Single writer per path`, decided 2026-11-07, becomes `2026-11-07-single-writer-per-path.md`.
7. Link the note from the touched code or document and the PR body. Use a relative markdown link or the repo-relative form `See .agents/notes/...` that [notes README](../../notes/README.md) uses; from nested files add the matching `../` depth, and link any superseded record from `## References` with the same relative style.
8. Keep the status truthful. A note created before the decision is agreed or shipped stays in `proposed/`; the PR that lands it moves it to `implemented/` and updates the `Status:` line. When a newer decision replaces it, move it to `archived/`, update `Status:`, add a "Superseded by" link in `## References`, and keep the file. The Lead owns the `git mv` and the commit; never delete a record.
9. Validate the result: `node scripts/scaffold-check.mjs` locally; the `.github/workflows/agents-scaffold.yml` job runs the same check in CI.

## Decide
| Situation | Action |
|---|---|
| An existing note already records the decision | Update or link it; never open a duplicate that can drift. |
| Decision agreed but not shipped yet | Keep it in `proposed/`; move to `implemented/` in the shipping PR. |
| A newer decision replaces it | Move to `archived/` with a superseded-by link; keep the file. |
| The decision must bind future changes | Encode the rule in AGENTS.md or the spec and link the note as rationale. |
| One-file change inside an established pattern | Skip the note; a clear commit message is enough. |
| Bug fix that restores specified behavior | Note it only when the fix changes a convention or exposes a systemic cause. |

## Verify
- [ ] Path matches `.agents/notes/<status>/<kind>/<YYYY-MM-DD>-<kebab-slug>.md`, both values are allowed, and the slug is unique.
- [ ] The file follows [TEMPLATE.md](../../notes/TEMPLATE.md) headings, every heading is answered, and the `Status:` and `Date:` lines match the directory and decision date.
- [ ] Every alternative names why it was rejected; consequences separate positive, negative, and neutral effects; follow-ups name an owner and trigger.
- [ ] Relative links resolve and the note links any superseded record.
- [ ] The touched file or PR links the note.
- [ ] `node scripts/scaffold-check.mjs` passes.
- [ ] Status matches reality: `proposed` before merge, `implemented` after, `archived` only with a superseding link.

## Pitfalls
- Treating a note as a specification; code and specs win, and the note only explains.
- Writing the note days later from memory; rationale degrades fast, so capture it at decision time.
- Duplicating an existing note, which creates two truths that drift apart.
- Deleting a superseded note; archive it so readers see why the decision changed.
- Absolute local paths or chat links as references; they do not resolve in another checkout.
- Packing several decisions into one note; split it, one decision per file.

## References
- [notes README](../../notes/README.md) — index, statuses, kinds, and linking conventions.
- [notes template](../../notes/TEMPLATE.md) — the headings to copy and fill.
- [git-review skill](../git-review/SKILL.md) — the reviewer checks that a note exists for cross-cutting choices.
- [scaffold check](../../../scripts/scaffold-check.mjs) — validates the note path, frontmatter, and links.
