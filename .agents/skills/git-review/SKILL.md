---
name: git-review
description: Use when reviewing a change set before merge — gather the diff, judge it in priority order, and report blocking versus non-blocking findings with concrete fixes. Review the change, not the author.
---

# Git review

Review a branch or pull request as the last gate before merge and return an actionable verdict.

## Use this when
- A branch or PR claims to be ready and needs an explicit approve or request-changes verdict.
- A `reviewer` teammate reviews under the self-contained card in [reviewer](../../agents/reviewer.md).
- Not for fixing the change; the reviewer reports, the implementer edits.
- Not for generated output or vendored code; review the generator input, lockfile entries, or upstream release instead.

## Procedure
1. Pin the range. Read `git log --oneline main..HEAD` and `git diff --stat main...HEAD`, then `gh pr view <number>` and `gh pr diff <number>`. Three dots (`main...HEAD`) start at the merge base; two dots include commits that main already has after a merge.
2. Read the patch as files: `git diff main...HEAD -- <path>` per changed file, then open the full file around each hunk. Confirm the PR body follows `.github/pull_request_template.md` and matches the diff.
3. Run what you can without changing the branch: the test and lint commands named in AGENTS.md or the PR body, `node scripts/scaffold-check.mjs` when the scaffold, skills, or notes changed, and `git diff --check main...HEAD` for whitespace errors.
4. Judge in this priority order, stopping early only when a blocking defect is already proven:
   1. Correctness: logic, edge cases, empty and null input, ordering, concurrency, idempotency.
   2. Tests: every behavior change has a test that fails before the fix and passes after; tests assert behavior, not internals.
   3. Security and secrets: no credential, token, or key literals added; no secrets logged; input validated at trust boundaries; no injection path.
   4. Error handling: failures surface instead of being swallowed; retries bounded; partial failure cleans up.
   5. Data migration and rollback: schema and format changes are reversible or forward-compatible; deploy order is safe.
   6. Docs and AGENTS.md freshness: README, AGENTS.md, and linked notes stay true after the change.
   7. Style and naming: raise only when it hides one of the above.
5. Report findings in one format, each with evidence:
   - blocking: `path/to/file:42` — problem. Fix: concrete change. Evidence: failing test, command output, spec line.
   - non-blocking: `path/to/file:7` — problem. Fix: concrete change.
6. State the verdict: request changes when at least one blocking finding exists; approve with notes when there is no blocking finding but at least one non-blocking note for a follow-up; approve only when there are no findings. "Looks fine" without the evidence above is not a review.
7. Re-review fixes by diff only: record `git rev-parse HEAD`, then read `git diff <reviewed-sha>...HEAD`; confirm every finding is resolved and no unrelated change slipped in. Repeat until the verdict is approve.
8. The Lead copies the [reviewer card](../../agents/reviewer.md) into the delegation prompt; the card reports `## Status` as `pass` or `changes requested`, which map to approve or request changes, and its non-blocking findings are the notes in approve with notes. The Lead merges only after the verdict, green checks, and explicit user intent, with a squash merge; the reviewer never merges or force-pushes.

## Decide
| Situation | Action |
|---|---|
| Diff is too large to review honestly | Request changes: split by concern and keep mechanical renames separate from behavior. |
| Generated, vendored, or lock files changed | Review the generator input or lockfile entries; skip hand-reading generated output. |
| Fix commit touches files outside the findings | Treat it as a new change and review those hunks before approving. |
| CI red, build broken, or a test skipped | Request changes regardless of code quality. |
| Only non-blocking notes remain | Approve with notes; route each note to the task board or a follow-up. |

## Verify
Copy this checklist into the review report and tick every line; an unticked line blocks approval.
- [ ] Reviewed range is printed: `git log --oneline main..HEAD` and `git diff --stat main...HEAD`.
- [ ] Every behavior change has a test that fails without the diff.
- [ ] Secret scan finds no new literal: `git grep -nE '(api[_-]?key|secret|token|password|private[_-]?key)[[:space:]]*[:=]' HEAD`.
- [ ] `gh pr checks <number>` reports every check passing.
- [ ] Commit subjects and the PR title follow the Conventional Commits rules in [git-commit](../git-commit/SKILL.md); `scripts/check-commit-message.sh` is the validator used by the hook and CI.
- [ ] A decision note exists for any cross-cutting choice, per [agent-notes](../agent-notes/SKILL.md).
- [ ] Each finding carries `path:line`, a concrete fix, and evidence.
- [ ] Verdict is exactly one of: request changes, approve with notes, approve.

## Pitfalls
- Reviewing `main..HEAD` instead of `main...HEAD`; after a merge it shows commits that are not part of the change.
- Trusting the PR description instead of the diff; description drift is itself a finding.
- Treating green CI as a verdict; CI does not prove the feature behaves as specified.
- Editing the branch mid-review, which changes the commit under review.
- Force-pushing between `git rev-parse HEAD` and the re-review; re-read the SHA before approving.

## References
- [reviewer card](../../agents/reviewer.md) — self-contained card the Lead copies into the delegation prompt.
- [git-commit skill](../git-commit/SKILL.md) — Conventional Commits subject rules that PR titles reuse.
- [git-worktree skill](../git-worktree/SKILL.md) — reviewing a branch that lives in a linked worktree.
- [PR template](../../../.github/pull_request_template.md) — the body checklist verified in step 2.
