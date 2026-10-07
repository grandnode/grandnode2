#!/usr/bin/env bash
# PreToolUse hook: refuse `git commit` / `git push` that would land on develop or main.
# Work goes on a feature branch and reaches develop/main through a pull request.
# Exit 2 blocks the tool call and hands the message on stderr back to the agent.

input=$(cat)

# Only git commit / git push are of interest; everything else passes straight through.
printf '%s' "$input" | grep -Eq 'git( -C [^ ]+)? (commit|push)' || exit 0

branch=$(git -C "${CLAUDE_PROJECT_DIR:-.}" branch --show-current 2>/dev/null)

if [[ "$branch" == "develop" || "$branch" == "main" ]]; then
  echo "Blocked: the current branch is '$branch'. Create a feature branch first (git switch -c feature/<name> or fix/<name>) and open a pull request to develop." >&2
  exit 2
fi

# A push from a feature branch that targets develop/main explicitly, e.g. `git push origin develop` or `HEAD:main`.
# Matched as `git push [flags] [remote] [src:]develop|main` so prose mentioning both words does not trip it.
if printf '%s' "$input" | grep -Eq 'git( -C [^ ]+)? push( +-[^ "\\]+)*( +[^- "\\][^ "\\]*)? +([^ "\\]*:)?(develop|main)([ "\\]|$)'; then
  echo "Blocked: pushing to develop/main directly. Push the feature branch and open a pull request instead." >&2
  exit 2
fi

exit 0
