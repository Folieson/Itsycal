---
name: git-commit
description: Use when writing, amending, or rewriting a commit message — write a Conventional Commits header that the commit-msg hook and CI accept, plus a body that explains why the change exists.
---

# Commit messages

One Conventional Commits header per commit; the hook and CI reject anything else.

## Use this when
- Writing any commit message, including `git commit -m` from an agent.
- Fixing, rewording, or squashing commits before they are pushed or reviewed.
- Not for branch, PR, or merge mechanics (`.agents/skills/git-workflow/SKILL.md`) or safe history rewriting after a push (`.agents/skills/git-sync/SKILL.md`).

## Procedure
1. Choose the type from the Decide table and the narrowest scope. The scope is the affected area or package, lowercase, for example `(auth)`, `(scaffold)`, `(deps)`; omit it when the change crosses areas.
2. Write the header in this grammar:
   ```text
   <type>(<scope>)!: <subject>
   ```
   - `<type>` is required and is one of `feat fix docs style refactor perf test build ci chore revert`.
   - `(<scope>)` is optional; lowercase `[a-z0-9._/-]` only.
   - `!` is optional and marks a breaking change; pair it with a `BREAKING CHANGE:` footer.
   - `<subject>` is required: imperative mood, lowercase first letter, no trailing period, at most 72 characters.
3. Add a body only when the header cannot carry the reason. Put one blank line after the header, explain why the change is needed and the tradeoff it takes, and wrap at 72 columns. Reviewers read `git show`, not your intent.
4. Add footers after a blank line, one per line as `Token: value`:
   ```
   Refs #34
   Closes #35
   BREAKING CHANGE: /v1/session now requires a token; migrate to /v2/session.
   ```
   Issue references belong in footers, never in the subject.
5. Let the validator gate it. `.githooks/commit-msg` runs `scripts/check-commit-message.sh` on the first non-comment, non-blank line. It accepts `<type>(<scope>)!: <subject>` with an allowed type, an optional lowercase `[a-z0-9._/-]` scope, and a non-empty subject; it rejects an unknown or missing type, a malformed or uppercase scope, a subject starting uppercase or ending in a period, and a subject longer than 72 characters. `Merge `, `Revert `, `fixup! `, `squash! ` prefixes and `#` comments are exempt. Enable it once with `git config core.hooksPath .githooks` (`.githooks/README.md`); CI re-runs the script on the PR.
6. Clean up agent WIP before it is pushed:
   - `git add <path>` then `git commit --amend --no-edit`; `git commit --amend -m '<new header>'` to fix the message.
   - Fold checkpoints from the branch base with `git rebase -i origin/main`, marking early commits `reword` or `fixup`; continue with `git rebase --continue`, abort with `git rebase --abort` (`.agents/skills/git-recovery/SKILL.md`).
   - Amend or reword only commits that were not pushed to a shared branch. After rewriting a branch you alone own, push with `--force-with-lease` (`.agents/skills/git-sync/SKILL.md`).
   - The hook rejects `wip:` and every other unknown type. For a checkpoint, commit with a valid type and fold it before opening the PR.

## Decide
| Change | Type |
|---|---|
| New user-visible capability | `feat` |
| Corrects wrong behavior | `fix` |
| Documentation only | `docs` |
| Formatting only, no behavior | `style` |
| Restructure with no behavior change | `refactor` |
| Measurably faster or smaller | `perf` |
| Adds or fixes tests only | `test` |
| Build system, dependencies, packaging | `build` |
| CI configuration and workflows | `ci` |
| Maintenance with no source behavior change | `chore` |
| Reverts an earlier commit | `revert` |

Worked examples:

| Message | Verdict | Reason |
|---|---|---|
| `feat(auth): add retry on 429 responses` | good | valid header |
| `fix(api): reject empty ids in batch endpoint` | good | fixes behavior |
| `docs(readme): document the scaffold check` | good | docs only |
| `build(deps): bump glob to 10.4.5` | good | dependency bump |
| `feat(auth)!: require session tokens on admin routes` | good | breaking marker |
| `Fix: Fixed the Login.` | bad | uppercase type, past tense, trailing period; use `fix(auth): reject expired session tokens` |
| `update stuff` | bad | no type; use `docs(readme): refresh setup notes` |
| `refactor: add export command` | bad | adds behavior, so it is `feat`, not `refactor` |

## Verify
- [ ] `git log -1 --pretty=%B` shows one header line and a blank line before any body or footer
- [ ] the full header obeys the grammar and subject rules above, and the body explains why
- [ ] `!` is paired with a `BREAKING CHANGE:` footer; `Closes #` and `Refs #` values are real issue numbers
- [ ] the commit passed `.githooks/commit-msg` (`git config core.hooksPath` prints `.githooks`) or the scaffold CI check is green
- [ ] rewrite and amend steps touched only unpushed commits: `git log --oneline origin/main..HEAD`

## Pitfalls
- Uppercase `Feat:` or a missing space after the colon; the validator rejects both.
- A ticket id is not a scope; `fix(JIRA-123)` is rejected, so use a footer `Refs #123`.
- The hook is local config; a fresh clone commits unchecked until `core.hooksPath` is set, and CI then rejects the PR.
- Amending after review replaces the SHA; comments and approvals still point at the old commit, so push a new commit instead.
- A body without a blank line after the header merges into the first line and can push it over 72 characters.

## References
- `.githooks/commit-msg` — hook git runs with the message file.
- `.githooks/README.md` — one-time `core.hooksPath` setup.
- `scripts/check-commit-message.sh` — validator shared by the hook and CI.
- `.github/workflows/agents-scaffold.yml` — CI that re-runs the validation.
- `.agents/skills/git-workflow/SKILL.md` — branch and PR flow that consumes the message.
- `.agents/skills/git-sync/SKILL.md` — force-with-lease after a rewrite.
- `.agents/skills/git-recovery/SKILL.md` — abort a rebase or undo an amend.
