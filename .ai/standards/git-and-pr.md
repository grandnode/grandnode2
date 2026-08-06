# Standard: Git and Pull Requests

---

## Branches

- `main` — released code.
- `develop` — integration branch; feature work targets `develop` unless told otherwise.
- Feature branches: `feature/{short-description}`, fixes: `fix/{short-description}`.

Confirm the base branch before opening a PR. Most contributions go to `develop`.

## Commits

- One logical change per commit. A refactor and a behavior change belong in separate commits.
- Imperative subject line, under ~72 characters, describing the effect: `Add store scope to payment restrictions`.
- Body explains *why*, not *what* — the diff already shows what.
- Do not commit `obj/`, `bin/`, `TestResults/`, `.vs/`, `.idea/`, or IDE user files.
- Generated frontend bundles **are** committed, alongside the source that produced them.

## Pull requests

`PULL_REQUEST_TEMPLATE.md` at the repository root is mandatory. Fill in every section:

```
Resolves #issueNumber
Type: **feature|bugfix**

## Issue
Description of the issue this PR is solving, why it's happening, and how to reproduce it.

## Solution
Summarize your solution to the problem.

## Breaking changes
List them, or state none.

## Testing
Numbered, reproducible steps. Assume the reader can already run GrandNode.
```

Rules:

1. Link an issue. If none exists, describe the problem in the Issue section as if one did.
2. State `Type:` as exactly one of feature or bugfix.
3. "Breaking changes: none" is an assertion — verify it. Changing a view model, removing a widget zone, renaming a plugin system name, or changing a public interface is breaking.
4. Testing steps must be executable by someone who did not write the change.
5. Keep the diff scoped to the stated issue. Unrelated formatting churn makes review harder and gets PRs rejected.

## Before opening a PR

- [ ] Solution builds.
- [ ] The affected test project passes.
- [ ] New user-facing strings exist as localization resources with an upgrade migration.
- [ ] New settings have defaults that preserve existing behavior.
- [ ] New permissions have a `PermissionProvider` entry and a migration.
- [ ] Generated bundles are rebuilt and committed if frontend source changed.
- [ ] `.ai/prompts/review-change.md` run against the diff.

## Working in someone else's branch

- Never overwrite unrelated local changes.
- Never force-push a shared branch.
- Bot-authored branches (e.g. Copilot agent branches) may receive further automated pushes — branch off them rather than committing directly, unless the change is meant to land in that PR.
