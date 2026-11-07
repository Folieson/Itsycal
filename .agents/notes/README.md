# Decision records

A decision record captures one decision: the context, the choice, the alternatives that
lost, and what the choice costs. Records give the next reader the reasoning the code
cannot show.

Records are non-authoritative working context. Code, tests, and specs win when they
disagree with a record; a record that contradicts the code is a bug to fix, not a spec to
follow.

## Path and name

`.agents/notes/<status>/<kind>/<YYYY-MM-DD>-<kebab-slug>.md`

| Part | Allowed values |
|---|---|
| `<status>` | `proposed`, `implemented`, `archived` |
| `<kind>` | `architecture`, `feature`, `bug-fix`, `simplification`, `process`, `testing` |
| `<YYYY-MM-DD>` | date the decision was made, UTC |
| `<kebab-slug>` | three to six lowercase words joined by hyphens, unique in the repository |

Example: `.agents/notes/implemented/architecture/2026-02-14-queue-ownership.md`.

Every status directory contains a `.gitkeep` so the layout exists in a fresh clone; leave
those files in place.

## Finding records

`git ls-files .agents/notes` lists every record; the status and kind directories group
them by lifecycle stage and decision type. Search before writing:
`git grep -n -i "<topic>" -- .agents/notes`.

## Statuses

| Status | Meaning | Next state |
|---|---|---|
| `proposed` | Drafted; not yet implemented or agreed | `implemented` when the change lands |
| `implemented` | In effect; the code follows it | `archived` when superseded or removed |
| `archived` | No longer applies; kept for history | none |

## Kinds

- `architecture` — structural boundaries, ownership, data flow, cross-cutting contracts.
- `feature` — user-visible behavior and its scope.
- `bug-fix` — root cause and why this fix.
- `simplification` — removal or consolidation that changes the design.
- `process` — how agents and humans work in this repository.
- `testing` — test strategy, fixtures, or coverage decisions.

## When to write

- The decision is hard to reverse or expensive to revisit.
- A reasonable engineer seeing only the code would ask "why this way?".
- Two alternatives were plausible and the rejection needs a reason.
- The choice constrains future agents: boundaries, ownership, tooling, public API.

## When to skip

- Typo, formatting, dependency bump, or rename with no design weight.
- The commit message and diff already say everything.
- `AGENTS.md` or a skill already states the rule; link to it instead.
- Speculation without a decision; write the record when the decision exists.

## Lifecycle

1. Search `.agents/notes/` for an existing record; update or link it instead of opening a
   duplicate. Then copy `.agents/notes/TEMPLATE.md` into `proposed/<kind>/` with the exact
   name format.
2. Move it to `implemented/` in the change that implements it, and update the Status line.
3. Move it to `archived/` when superseded, add a `Superseded by:` line under the title,
   and link the superseding record.
4. Never delete a record and never reuse its path. Archive instead; history is the point.

Status moves are plain file moves; the Lead owns the `git mv` and the commit.

## Linking

- Code and docs: `See .agents/notes/implemented/architecture/2026-02-14-queue-ownership.md`.
- Links from nested files are relative markdown links with the matching `../` depth.
- PR description: `Notes: .agents/notes/proposed/process/2026-02-14-review-gates.md`.
- A superseding record lists the old path under `## References`.
- Link from where the next reader is, such as the module header near the decision, not
  only from the PR.

## References

- `.agents/notes/TEMPLATE.md` — copy-paste skeleton for a new record.
- `.agents/skills/agent-notes/SKILL.md` — procedure for writing, moving, and archiving
  records.
- `scripts/scaffold-check.mjs` — validates note paths and headings locally and in CI.
