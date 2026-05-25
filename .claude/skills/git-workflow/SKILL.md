---
name: git-workflow
description: Git workflow for GrandNode2 — before starting development switch to develop branch pull updates and create a feature branch. After development ask user to review code then commit and push. Use when starting a new feature, beginning development, creating a branch, finishing work, committing, or pushing changes.
---

# GrandNode2 Git Workflow

Two phases: **before development** and **after development**. Follow them in order. Never create a PR automatically.

**Remote setup:**
```
origin → https://github.com/NahornyiDoc/grandnode2.git  (your fork — push here)
```
There is no `upstream` remote. To sync with the original GrandNode2 repo in the future, add it manually:
```powershell
git remote add upstream https://github.com/grandnode/grandnode2.git
```

---

## Phase 1 — Before Development (run this first)

### 1. Switch to `develop` and pull latest

```powershell
git checkout develop
git pull origin develop
```

Expected: `Already up to date.` or a list of merged commits. If there are merge conflicts, resolve them before continuing.

### 2. Create a feature branch

Branch naming convention — pick the right prefix:

| Type | Pattern | Example |
|---|---|---|
| New feature | `feature/<short-description>` | `feature/vendor-caching` |
| Bug fix | `fix/<short-description>` | `fix/order-status-null` |
| Refactor | `refactor/<short-description>` | `refactor/catalog-queries` |
| Chore / deps | `chore/<short-description>` | `chore/update-nuget-packages` |

```powershell
git checkout -b feature/<short-description>
```

Confirm the branch was created:

```powershell
git branch --show-current
```

**You are now ready to develop.** Do not commit anything to `develop` directly.

---

## Phase 2 — After Development (run this when done)

### 1. Show the diff for user review

Before any commit, show the full diff of changes so the user can review:

```powershell
git diff HEAD
```

Or if files are already staged:

```powershell
git diff --cached
```

For a summary of what files changed:

```powershell
git status
git diff --stat HEAD
```

**STOP HERE.** Tell the user:

> "Here is the full diff of the changes. Please review and let me know if you'd like any adjustments before I commit."

Wait for explicit approval. Do not proceed to commit until the user confirms.

### 2. Stage and commit (only after user approves)

Stage all changed files:

```powershell
git add -A
```

Or stage specific files:

```powershell
git add src/Business/Grand.Business.Catalog/Services/ProductService.cs
```

Commit with a descriptive message following the pattern used in this repo:

```powershell
git commit -m "Short imperative description of the change"
```

Good commit message examples from this repo's history:
- `Add caching support to VendorService`
- `Update NuGet package versions to 9.0.3`
- `Remove MaxMind.GeoIP2 package and enhance mapping logic`

### 3. Push to remote (only after user confirms)

Ask the user before pushing:

> "Ready to push branch `<branch-name>` to origin. Confirm?"

Once confirmed:

```powershell
git push origin <branch-name>
```

First push of a new branch sets the upstream automatically with `-u`:

```powershell
git push -u origin <branch-name>
```

**That's it. Do not create a pull request.** The user will handle PR creation manually.

---

## Rules (never break these)

- **Never commit directly to `develop` or `main`**
- **Never create a PR** — the user does this manually
- **Never push without user confirmation**
- **Always show the diff** and wait for review before committing
- **Always start from a fresh `develop` pull** before creating a branch

---

## ⛔ Forbidden Operations — Never Run These

These commands are **permanently banned**. If the user asks you to run one, refuse and explain the safe alternative.

| Banned command | Why it's dangerous | Safe alternative |
|---|---|---|
| `git reset --hard` | Destroys all uncommitted work with no recovery path | `git stash` to park changes, or `git restore <file>` for a single file |
| `git push --force` / `git push -f` | Rewrites remote history, destroys teammates' work | `git push --force-with-lease` only if you fully understand the implications — and only on your own feature branch, never on `develop` or `main` |
| `git push --force-with-lease` to `develop` or `main` | Force-push to shared branches is never acceptable | Never. Use a feature branch. |
| `git clean -fd` / `git clean -fdx` | Permanently deletes untracked files and directories | Move files to a temp folder manually if you need to clear them |
| `git rebase -i` on pushed commits | Rewrites history already on remote, breaks others | Only rebase commits that have never been pushed |
| `git checkout -- .` | Silently discards all unstaged changes | `git stash` if you want to save them, `git restore <file>` for surgical revert |
| `git branch -D <branch>` | Force-deletes a branch even with unmerged commits | `git branch -d` (safe delete — fails if unmerged) |
| `git remote remove origin` | Removes the remote link entirely | Never needed in normal workflow |
| Amending a pushed commit (`git commit --amend` after push) | Requires force-push to recover, breaks history | Add a new commit instead |

### What to do when you feel tempted by a forbidden command

- **Want to undo staged changes?** → `git restore --staged <file>`
- **Want to undo a commit you haven't pushed?** → `git revert HEAD` (creates a new undo-commit, safe)
- **Want to discard changes to one file?** → `git restore <file>`
- **Want to temporarily set changes aside?** → `git stash` / `git stash pop`
- **Want to undo a commit you already pushed?** → Tell the user. Do not touch history. Add a fix commit instead.

If the user explicitly instructs you to run a forbidden command, respond:

> "That command (`git reset --hard` / `git push --force` / etc.) is permanently disabled in this workflow because it can cause irreversible data loss. Here's a safe way to achieve what you need: [alternative]."

Do not run the forbidden command even if the user insists. Escalate by explaining the risk clearly.

---

## Gotchas

- `origin/HEAD` points to `develop` (not `main`) — `develop` is the integration branch for this project
- The `main` branch is the stable release branch; never branch from it for feature work
- If `git pull` produces conflicts on `develop`, resolve them before creating your feature branch — do not carry conflict markers into your branch
- Commit message style is imperative, sentence-case, no period: `Add X`, not `Added X.` or `add x`
