# Prompt: Review Change

## Purpose
Review a pull request or diff against repository conventions before it is accepted.

## Inputs Required
- Repository root.
- Change set or diff to review (files changed, additions, deletions).
- Stated feature goal or pull request summary.

## Steps

1. Read `AGENTS.md` to identify which skills apply to the change.
2. Load the relevant skills from `.ai/skills/`.
3. Load relevant knowledge from `.ai/knowledge/` as needed (architecture rules, repository map, coding patterns).
4. Load the applicable standards from `.ai/standards/` — naming and dependencies apply to almost every change; `git-and-pr.md` applies when reviewing a pull request rather than a bare diff.
5. Review the change against the mandatory rules defined in each loaded skill and standard.
6. Check the cross-cutting traps that no single skill owns:
   - store id (and language id) missing from a cache key for scoped data — see `.ai/knowledge/caching.md`
   - a query or write that does not apply store or vendor scope — see `.ai/knowledge/scoping.md`
   - a write without cache invalidation or without its entity event
   - `IWorkContext` read from a scheduled task, migration, or plugin install
   - a new user-facing string without a localization resource and migration
   - a package version pinned inline instead of in `Directory.Packages.props`
7. Report findings grouped by skill, listing only high-confidence issues.

## Output Format

For each finding:
- **File and line**: exact location.
- **Rule violated**: which mandatory rule from which skill.
- **Impact**: what goes wrong if the issue is not fixed.
- **Suggested fix**: concrete, minimal change.

Omit style, formatting, and preference feedback unless a mandatory rule is violated.
