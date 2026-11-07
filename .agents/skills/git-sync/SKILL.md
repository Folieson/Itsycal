---
name: git-sync
description: Use when a branch or pull request is behind `main`, a rebase or merge stops with conflicts, or a pushed branch must be updated — rebase or merge deliberately, resolve, re-verify, and push with lease.
---

# Keep a branch current

Bring new `main` commits into a branch without rewriting shared history or losing work.

## Use this when
- The PR shows "behind" or "conflicts" because `main` moved.
- A rebase, merge, or cherry-pick stopped with conflicts.
- Resuming a long-running PR after a pause.
- Not for branch creation and landing (`.agents/skills/git-workflow/SKILL.md`), undoing a bad operation (`.agents/skills/git-recovery/SKILL.md`), or message wording (`.agents/skills/git-commit/SKILL.md`).

## Procedure
1. Fetch and inspect before touching anything:
   ```
   git fetch --prune origin
   git status --short --branch
   git log --oneline HEAD..origin/main
   ```
   A rebase needs a clean tree; commit the work or stash it first.
2. Choose rebase or merge with the Decide table below.
3. Rebase a branch only you push to:
   ```
   git rebase origin/main
   ```
   Resolve conflicts with the protocol below, then update the remote once:
   ```
   git push --force-with-lease origin HEAD
   ```
4. Merge a shared branch or a hotfix:
   ```
   git merge origin/main
   git push origin HEAD
   ```
   Resolve conflicts with the same protocol, then conclude with `git commit`.
5. Conflict protocol, identical for rebase and merge:
   - Stop and list the conflicts: `git status` and `git diff`; read both sides in the file markers rather than guessing.
   - Inspect why both sides changed the file: `git log --merge --oneline -- <path>`.
   - Edit to the intended final content. Do not accept one side wholesale with `git checkout --ours` or `--theirs` unless you inspected the diff.
   - Stage the resolution: `git add <path>`; run the repository verification command before continuing.
   - Continue with `git rebase --continue` or `git commit` for a merge. Abort with `git rebase --abort` or `git merge --abort`; both return to the pre-operation state (`.agents/skills/git-recovery/SKILL.md`).
6. Re-verify after a rebase: rebasing rewrote the commits, so run the full test, typecheck, and lint commands on the final tree, inspect `git diff origin/main...HEAD` for unintended drift, and let CI run again on the pushed branch.
7. Refresh a long-running PR by merging `origin/main` into the branch (rebase only if nobody else has commits on it), then confirm with `gh pr view --json state,mergeable`.

## Decide
| Situation | Action |
|---|---|
| Branch only you push to | rebase onto `origin/main` |
| Someone else pushed to the branch or reviews its commits | merge `origin/main` into it |
| Long-running PR refreshed periodically | merge `origin/main` into it |
| Hotfix branch cut from `main` | merge; never rebase a hotfix under review |
| Conflicts in lockfiles, generated files, snapshots | regenerate with the project command; do not hand-merge |
| PR already approved and checks are green | do not rewrite; merge current `main` only if branch protection requires it |
| Conflict resolution needs product judgment | stop and ask the user; do not invent behavior |

## Verify
- [ ] `git status --short --branch` clean, ahead N, behind 0
- [ ] no markers left: `git grep -nE '^(<{7}|>{7})'` returns nothing
- [ ] repository verification command passes on the final tree
- [ ] `git log --oneline origin/main..HEAD` contains the expected commits and no duplicates
- [ ] any force-push used `--force-with-lease` and only on a branch you own
- [ ] PR checks are green again after the push

## Pitfalls
- Plain `git pull` uses the configured strategy and can add merge noise; fetch, then rebase or merge explicitly.
- Rebasing a branch under review dismisses approvals or invalidates review comments on some forges; prefer merge there.
- `--ours` and `--theirs` are inverted during a rebase relative to a merge; never use them without checking `git status`.
- `git push --force` can erase a teammate's commits; `--force-with-lease` refuses when the remote moved.
- A rebase that resolved conflicts can still break tests, because each rewritten commit is not independently tested; verify the tree, not the commits.
- After a rebase, generated files and dependencies may need reinstalling before tests pass.

## References
- `.agents/skills/git-recovery/SKILL.md` — abort commands and reflog recovery.
- `.agents/skills/git-workflow/SKILL.md` — PR flow and squash merge.
- `.agents/skills/git-commit/SKILL.md` — rewrite messages while squashing.
- `.agents/agents/verifier.md` — post-rebase verification brief.
- `.github/pull_request_template.md` — PR body and check list.
