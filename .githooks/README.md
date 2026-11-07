# Git hooks

Enable them once per clone:

```sh
git config core.hooksPath .githooks
```

Disable temporarily: `git config --unset core.hooksPath`.
On Linux/macOS, make the files executable if your checkout lost the bit:
`chmod +x .githooks/commit-msg .githooks/pre-commit`.

## `commit-msg`

Validates the first non-comment line against Conventional Commits by calling
`scripts/check-commit-message.sh`. It accepts `Merge …`, `Revert "…"`, `fixup! …`, and
`squash! …`; everything else needs `<type>(<scope>)!: <subject>`, at most 72 characters,
no trailing period, and no capital first letter in the subject. Exit code 1 rejects the
commit. Editing the message in `git commit --amend` re-runs the hook.

## `pre-commit`

Blocks the mistakes that are cheap to catch locally: conflict markers in staged content,
staged `AGENTS.local.md` / `CLAUDE.local.md`, staged paths under `.agents/worktrees/` or
`.dsh-tmp/`, and staged files larger than 5 MB. It reads the index only and needs no
Node.js, so it stays fast.

## `pre-push`

Guards `main` against the two destructive pushes that a free private repository cannot
block server-side: deleting the branch and a non-fast-forward push that rewrites history.
Fast-forward pushes and pushes to any other branch pass untouched. This is the local
substitute for the "no force pushes, no deletions" part of branch protection; see
`.agents/notes/implemented/process/2026-11-07-branch-protection-fallback.md`.

Deliberate bypass: `git push --no-verify` — only for recovery situations where the user
explicitly asked for the rewrite.

## Bypassing

`git commit --no-verify` skips both hooks. That is acceptable only when the hook itself is
wrong or blocked — never to land a malformed message or a staged secret. CI runs
`scripts/scaffold-check.mjs` and the same commit-message validator, so a bypass shows up
as a red check on the pull request.
