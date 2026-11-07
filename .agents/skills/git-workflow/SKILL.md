---
name: git-workflow
description: Use when starting, reviewing, or landing a change against protected `main` — choose a branch type, keep one task per branch, open a PR, and squash-merge it with `gh`. Commit wording is in git-commit.
---

# Git workflow

Land every change through a short-lived branch and a pull request; `main` stays releasable.

## Use this when
- Starting any task that changes tracked files, even a one-commit fix.
- Choosing a branch name, opening or updating a PR, merging, or cleaning up after a merge.
- Not for commit message wording (`.agents/skills/git-commit/SKILL.md`), keeping a stale branch current (`.agents/skills/git-sync/SKILL.md`), or undoing a mistake (`.agents/skills/git-recovery/SKILL.md`).

## Procedure

### Start a task
1. Confirm a clean tree and current `main`:
   ```
   git status --short --branch
   git switch main
   git pull --ff-only
   ```
2. Cut a short-lived branch from that `main`:
   ```
   git switch -c feat/<slug>
   ```
   Use a lowercase kebab slug of two to five words (`feat/login-retry`). One task per branch; unrelated work gets its own branch or its own worktree (`.agents/skills/git-worktree/SKILL.md`).
3. Work in small, coherent, buildable increments. Before each commit, run the repository verification command (tests, typecheck, lint) for the affected area.
4. Commit each increment with a valid header (`.agents/skills/git-commit/SKILL.md`).
5. Push and open the pull request:
   ```
   git push --set-upstream origin HEAD
   gh pr create --fill
   ```
   `--fill` copies commit subject and body. Pass `--title` and `--body` explicitly when the commit title is not a good PR title, and fill `.github/pull_request_template.md`.

### Finish a task
1. Keep the branch current and resolve conflicts per `.agents/skills/git-sync/SKILL.md`.
2. Wait for review and green checks:
   ```
   gh pr checks --watch
   ```
3. Merge only when checks are green and the user asked for the merge. Squash merge and delete the branch:
   ```
   gh pr merge --squash --delete-branch
   ```
4. Return to `main` and confirm the merge landed:
   ```
   git switch main
   git pull --ff-only
   gh pr view --json state,mergedAt
   ```
5. Delete a leftover local branch. After a squash merge, `git branch -d feat/<slug>` may refuse because the branch is not an ancestor of `main`; confirm the PR shows `MERGED`, then `git branch -D feat/<slug>`.

## Decide
| Situation | Branch | Integration |
|---|---|---|
| New capability | `feat/<slug>` | squash merge |
| Wrong existing behavior | `fix/<slug>` | squash merge |
| Docs, comments, README only | `docs/<slug>` | squash merge |
| Tooling, dependencies, CI, config | `chore/<slug>` | squash merge |
| Reverting an earlier PR | `fix/<slug>` or a `revert:` commit | squash merge |
| Every PR commit is releasable and the user wants the history | keep commits | rebase merge, only on user request |
| Branch shared with another person | never rewrite | merge the PR, no force-push |

## Verify
- [ ] `git status --short --branch` is clean and the branch matches `<type>/<slug>`
- [ ] `git log --oneline origin/main..HEAD` lists only this task's commits; `main` has no direct commits
- [ ] every commit header passes `.githooks/commit-msg` (enable with `git config core.hooksPath .githooks` per `.githooks/README.md`)
- [ ] `gh pr checks` reports every check passing
- [ ] the PR body follows the template and records the verification command that ran
- [ ] `gh pr view --json state` returns `MERGED`; local and remote branches are deleted

## Pitfalls
- Committing directly to `main` bypasses review and branch protection; always branch first and never force-push a shared branch.
- `gh pr create --fill` uses the first commit as the PR title; rewrite WIP commits before opening the PR.
- A squash merge drops the individual commits by design; do not look for them on `main`.
- Force-pushing after review hides what reviewers approved; push a new commit instead.
- `git pull` without `--ff-only` can create a merge commit on local `main`; always pull fast-forward.
- A branch older than a day usually needs a sync before the merge button is honest.

## References
- `.agents/skills/git-commit/SKILL.md` — header grammar the hook enforces.
- `.agents/skills/git-sync/SKILL.md` — rebase, merge, conflicts, force-with-lease.
- `.agents/skills/git-recovery/SKILL.md` — undo a bad commit before it lands.
- `.agents/skills/git-worktree/SKILL.md` — parallel tasks in separate checkouts.
- `.github/pull_request_template.md` — PR body checklist.
- `.agents/agents/reviewer.md` — review brief to hand to the reviewer.
- `.github/workflows/agents-scaffold.yml` — CI job that validates the scaffold.
