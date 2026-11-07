---
name: git-worktree
description: Use when parallel work must not share one checkout — create a DSH worktree, keep one branch and one writer per checkout, and remove it cleanly. Uncommitted work is not recovered.
---

# Git worktrees

Isolate each parallel writing task in its own checkout so concurrent agents never edit the same working tree.

## Use this when
- Two or more agents or teammates need to write code at the same time.
- A long change must not block the primary checkout or another agent.
- Not for a quick read-only inspection; stay in the current checkout.
- Not for updating an existing branch; use [git-sync](../git-sync/SKILL.md) instead.

## Procedure
1. Plan the isolation. One worktree per writing agent, one branch per worktree, no two agents in one checkout. The Lead creates the worktree and records disjoint write scopes on the task board; merging happens through pull requests, never by copying files between checkouts.
2. Name it. The name becomes the branch and the directory under `.agents/worktrees/`; follow the branch naming in [git-workflow](../git-workflow/SKILL.md), for example `feat/review-checklists`.
3. Choose the base. `from` accepts a local commit, branch, or tag; omit it for `HEAD`. Only local revisions resolve, so fetch first when the base is remote.
4. Create and enter it: call `create_worktree({ name: "feat/review-checklists", from: "main" })`. Success returns `path`, `branch`, `baseCommit`, and `repositoryRoot`, and switches the session working directory to `path`.
5. Prepare the checkout. Staged, unstaged, untracked, and ignored files are not copied, and dependencies are not installed. Run the project setup step recorded in AGENTS.md or README.md (for example `pnpm install` or `npm ci`) before running tests.
6. Work on exactly one branch; never create a second worktree for the same branch, and never share one checkout between two agents, because edits land in the same files. Commit as you go: uncommitted changes are not recovered by `restore()` and are lost when the checkout is removed.
7. Push the branch and open a PR with `gh pr create`. The Lead reviews it per [git-review](../git-review/SKILL.md) and squash-merges.
8. Leave with the working-directory tool: `working_directory({ cd: "<repositoryRoot>" })`. The checkout and branch remain until removed.
9. Clean up after merge, from the primary checkout: `git worktree list` to find the path, `git worktree remove .agents/worktrees/<name>`, then delete the merged branch. After a squash merge `git branch -d <branch>` refuses because the branch is not an ancestor of main; confirm the PR shows merged, then use `git branch -D <branch>`. A dirty worktree needs a commit first; remove only your own checkout.

## Decide
| Situation | Action |
|---|---|
| Two independent tasks | One worktree each from `main`, separate branches, merge each by PR. |
| Task needs an unmerged dependency | Wait for the dependency PR to merge, then branch from `main`; do not stack branches unless the Lead coordinates it. |
| Base branch moved while working | From inside the worktree: `git fetch origin` then `git rebase origin/main`; see [git-sync](../git-sync/SKILL.md). |
| Worktree left behind after a cancelled task | `git worktree list`, remove it from the primary checkout, then `git worktree prune`. |
| Branch or checkout path already exists | Pick a new name; `create_worktree` fails on an existing branch or path. |
| Need a second task in one checkout | Serialize the tasks; do not open a second branch in the same directory. |

## Verify
- [ ] `create_worktree` returned `path`, `branch`, `baseCommit`, and `repositoryRoot`, and the session directory is `path`.
- [ ] `git worktree list` shows the checkout and its branch.
- [ ] The setup step ran and the project test command passes inside the worktree.
- [ ] `git status` is clean and all work is pushed before leaving or handing off.
- [ ] After cleanup, `git worktree list` has no stale entry and the merged branch is deleted.

## Pitfalls
- Assuming uncommitted work follows you into the worktree; it does not, so commit or recreate it.
- Skipping the setup step: a fresh checkout has no `node_modules`, virtualenv, or build cache, so tests fail on imports, not logic.
- Running two agents in one checkout: writes interleave and the diff attributes nothing.
- Deleting the directory with `rm -rf`: Git keeps a stale registration; use `git worktree remove`.
- Expecting `restore()` to recover work: it recreates a removed checkout at its recorded commit only.
- Expecting filtered files to arrive hydrated: checkout filters are disabled, so Git LFS pointers stay pointers until hydrated separately.
- Reusing a branch name: every operation creates a new branch and an existing name is an error.

## References
- [git-workflow skill](../git-workflow/SKILL.md) — branch naming, one branch per task, merge policy.
- [git-sync skill](../git-sync/SKILL.md) — fetch and rebase to update the branch.
- [git-review skill](../git-review/SKILL.md) — review before the Lead merges.
- [implementer card](../../agents/implementer.md) — delegation card for the teammate working in the worktree.
- `.agents/worktrees/` — the gitignored pool of checkouts that the tool fills.
