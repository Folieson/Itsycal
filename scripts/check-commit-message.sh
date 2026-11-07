#!/bin/sh
# Conventional Commits validator, shared by .githooks/commit-msg and CI.
#
# Usage:
#   sh scripts/check-commit-message.sh <message-file>
#   sh scripts/check-commit-message.sh --message "<header line>"
#
# Validates the first non-comment, non-blank line of a commit message:
#   <type>(<scope>)!: <subject>
# Rules: known type, lowercase scope, non-empty subject, header <= 72 chars,
# subject does not start uppercase and does not end with a period.
# Auto-generated messages (Merge, Revert "...", fixup!, squash!) are accepted.
#
# Exit 0 when valid, 1 with an explanation on stderr when invalid.

set -u

usage() {
  printf 'usage: %s <message-file> | --message "<header>"\n' "$0" >&2
  exit 2
}

[ $# -ge 1 ] || usage

if [ "$1" = "--message" ]; then
  [ $# -ge 2 ] || usage
  header="$2"
else
  [ -f "$1" ] || { printf 'check-commit-message: no such file: %s\n' "$1" >&2; exit 2; }
  header=$(awk 'NF && $0 !~ /^#/' "$1" | head -n 1)
fi

if [ -z "$header" ]; then
  printf 'check-commit-message: empty commit message\n' >&2
  exit 1
fi

# Auto-generated messages that carry no human-authored header.
if printf '%s\n' "$header" | grep -Eq '^(Merge |Revert "|fixup! |squash! )'; then
  exit 0
fi

fail() {
  printf 'check-commit-message: %s\n' "$1" >&2
  printf '  got: %s\n' "$header" >&2
  printf '  expected e.g.: feat(cli): add dry-run flag\n' >&2
  printf '                 fix: guard empty config before parsing\n' >&2
  exit 1
}

pattern='^(feat|fix|docs|style|refactor|perf|test|build|ci|chore|revert)(\([a-z0-9._/-]+\))?!?: .+$'
printf '%s\n' "$header" | grep -Eq "$pattern" || fail "header is not Conventional Commits: <type>(<scope>)!: <subject>"

header_length=$(printf '%s' "$header" | wc -c | tr -d ' ')
[ "$header_length" -le 72 ] || fail "header is $header_length characters, limit is 72"

case "$header" in
  *.) fail "header must not end with a period" ;;
esac

subject=$(printf '%s\n' "$header" | sed 's/^[^:]*: //')
case "$subject" in
  [A-Z]*) fail "subject must not start with an uppercase letter" ;;
esac

exit 0
