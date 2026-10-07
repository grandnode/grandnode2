#!/usr/bin/env bash
# PreToolUse hook: refuse `git commit` / `git push` that would land on develop or main.
# Work goes on a feature branch and reaches develop/main through a pull request.
# Exit 2 blocks the tool call and hands the message on stderr back to the agent.
#
# The branch is read from the directory the command runs in (the hook's `cwd`, then any
# `cd <dir>` or `git -C <dir>` in the command), not from the project root, so a worktree
# on a feature branch is judged by its own branch.

input=$(cat)

# A JSON string field of the hook input, still escaped.
field() {
  printf '%s' "$input" | grep -oE "\"$1\" *: *\"([^\"\\\\]|\\\\.)*\"" | head -n 1 |
    sed -E "s/^\"$1\" *: *\"//; s/\"\$//"
}

command=$(field command)
[[ "$command" == *git* ]] || exit 0

# Unescape the JSON string: \\ -> \, \" -> ", \n -> newline.
command=$(printf '%s' "$command" | sed -E 's/\\\\/\x01/g; s/\\"/"/g; s/\\n/\n/g; s/\\t/ /g; s/\x01/\\/g')

cwd=$(field cwd | sed -E 's#\\\\#/#g')
dir=${cwd:-${CLAUDE_PROJECT_DIR:-.}}

# Resolve a path from the command against the directory the segment runs in.
resolve() {
  local p=${1//\\//}
  p=${p#[\"\']}; p=${p%[\"\']}
  case $p in
    /* | [A-Za-z]:*) printf '%s' "$p" ;;
    *) printf '%s/%s' "$2" "$p" ;;
  esac
}

protected() { [[ "$1" == "develop" || "$1" == "main" ]]; }

block() {
  echo "Blocked: $1 Create a feature branch first (git switch -c feature/<name> or fix/<name>) and open a pull request to develop." >&2
  exit 2
}

# Each shell segment (split on && || ; | and newlines) is judged on its own, so text inside
# another command, such as a PR body mentioning `git push origin develop`, does not count.
while IFS= read -r seg; do
  read -ra words <<< "$seg"
  [[ ${#words[@]} -gt 0 ]] || continue

  case ${words[0]} in
    cd | Set-Location)
      [[ -n ${words[1]} ]] && dir=$(resolve "${words[1]}" "$dir")
      continue ;;
    git) ;;
    *) continue ;;
  esac

  # Global options before the subcommand: -C <dir>, -c <key=value>, --no-pager and the like.
  gitdir=$dir
  i=1
  while [[ ${words[i]} == -* ]]; do
    case ${words[i]} in
      -C) gitdir=$(resolve "${words[i+1]}" "$gitdir"); i=$((i + 2)) ;;
      -c) i=$((i + 2)) ;;
      *) i=$((i + 1)) ;;
    esac
  done

  sub=${words[i]}
  [[ "$sub" == "commit" || "$sub" == "push" ]] || continue

  branch=$(git -C "$gitdir" branch --show-current 2>/dev/null)
  protected "$branch" && block "the current branch is '$branch'."

  [[ "$sub" == "push" ]] || continue

  # A push that names develop/main as the target: `origin develop`, `HEAD:main`, `+x:refs/heads/main`.
  remote_seen=
  for word in "${words[@]:i+1}"; do
    [[ $word == -* ]] && continue
    if [[ -z $remote_seen ]]; then remote_seen=1; continue; fi
    target=${word##*:}
    target=${target#+}
    target=${target#refs/heads/}
    protected "$target" && block "pushing to '$target' directly."
  done
done < <(printf '%s\n' "$command" | sed -E 's/&&|\|\||;|\|/\n/g')

exit 0
