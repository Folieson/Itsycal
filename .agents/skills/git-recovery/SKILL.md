---
name: git-recovery
description: Use when a commit, file, branch, or stash was lost or is about to be discarded — inspect first, then undo with the narrowest reversible command; destructive steps need explicit user intent.
---

# Recover and undo safely

Reverse a git mistake with the least destructive command, and know what cannot come back.

## Use this when
- Undoing a commit, unstaging files, discarding edits, or aborting a rebase, merge, or cherry-pick.
- A branch, commit, or stash disappeared.
- Not for rewriting unpushed history on purpose (`.agents/skills/git-commit/SKILL.md`), syncing with `main` (`.agents/skills/git-sync/SKILL.md`), or landing changes (`.agents/skills/git-workflow/SKILL.md`).

## Procedure
1. Inspect before changing anything, and record the SHA you may return to:
   ```
   git status --short --branch
   git reflog --date=iso -n 20
   git stash list
   git log --oneline -n 10
   ```
2. Pick the narrowest command in the Decide table.
3. Unstage only, keeping the worktree content:
   ```
   git restore --staged <path>
   ```
4. Discard one file's uncommitted edits only after reading the diff and receiving explicit user intent:
   ```
   git diff -- <path>
   git restore <path>
   ```
   This is destructive; the diff is the only record of what is lost.
5. Undo the last commit but keep its content, staged: `git reset --soft HEAD~1`. Use `git reset HEAD~1` to also unstage. Never `--hard` here.
6. Undo an amend: the pre-amend commit is in the reflog. Run `git reflog`, find its SHA, then `git reset --soft HEAD@{1}`.
7. Recover a deleted branch or commit:
   ```
   git reflog
   git switch -c <branch> <sha>
   ```
   For a commit not on any branch, make a rescue branch immediately: `git switch -c rescue <sha>`, or bring it back with `git cherry-pick <sha>`.
8. Recover a dropped stash:
   ```
   git fsck --no-reflogs --unreachable
   git stash apply <sha>
   ```
   The unreachable `commit` entries are the dropped stash commits.
9. Abort an in-progress operation; each returns to the state before it started:
   ```
   git rebase --abort
   git merge --abort
   git cherry-pick --abort
   git revert --abort
   ```
10. Stop. Never repair a mistake with another destructive command before the recovery is verified.

## Decide
| Need | Command | Reversible |
|---|---|---|
| Unstage a path | `git restore --staged <path>` | yes |
| Throw away one file's edits | `git restore <path>` | no |
| Undo one commit, keep work | `git reset --soft HEAD~1` | yes, via reflog |
| Undo an amend | `git reset --soft HEAD@{1}` | yes, via reflog |
| Recreate a deleted branch | `git switch -c <branch> <sha>` | yes |
| Abort rebase, merge, cherry-pick, revert | `git <op> --abort` | yes |
| Recover a dropped stash | `git stash apply <sha>` | yes |
| Undo a merged PR | `git revert <sha>` on a new branch | yes, as a new commit |

## Verify
- [ ] `git status --short --branch` shows the expected state and no operation in progress
- [ ] the recovered SHA is visible in `git reflog`
- [ ] recovered content matches the expectation: `git show <sha>` or `git diff <before> <after>`
- [ ] destructive steps were preceded by `git diff` output and explicit user intent
- [ ] any temporary rescue branch is named and known, not a detached HEAD

## Pitfalls

### Never do this
- No `git reset --hard` on work you did not inspect first.
- No `git restore <path>` or `git checkout -- <path>` without reading `git diff -- <path>`.
- No `git clean -fd` before `git clean -nd` lists the targets.
- No force-push to undo a merged commit on a shared branch; revert through a PR instead.
- No `git gc --prune=now` or reflog deletion while recovering.

### Not recoverable
- Uncommitted worktree edits discarded by `git restore`, `git checkout --`, or `git reset --hard`: they were never objects, so no reflog entry exists.
- Untracked files removed by `git clean -fd` or `git clean -fdx`: deleted from disk, no reflog.
- Reflog entries are local-only and expire (90 days by default); act before any `git gc`.
- Commits force-pushed away and garbage-collected on the remote may be gone from every clone.
- `git merge --abort` can fail with local modifications; stash or commit first and do not reach for `git reset --hard`.

## References
- `.agents/skills/git-commit/SKILL.md` — amend rules and accepted messages.
- `.agents/skills/git-sync/SKILL.md` — conflict handling and force-with-lease.
- `.agents/skills/git-workflow/SKILL.md` — revert through a PR.
- `.github/pull_request_template.md` — body for a revert PR.
