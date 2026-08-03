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
4. Review the change against the mandatory rules defined in each loaded skill.
5. Report findings grouped by skill, listing only high-confidence issues.

## Output Format

For each finding:
- **File and line**: exact location.
- **Rule violated**: which mandatory rule from which skill.
- **Impact**: what goes wrong if the issue is not fixed.
- **Suggested fix**: concrete, minimal change.

Omit style, formatting, and preference feedback unless a mandatory rule is violated.
