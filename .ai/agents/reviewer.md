---
name: Reviewer
description: Review a pull request or diff against all applicable repository skills and checklists before it is accepted.
tools: ["codebase", "search", "terminal"]
---

<!--
Adapter only. Full instructions live in AGENTS.md and .ai/prompts/review-change.md,
which remain the single source of truth. Do not duplicate rules here.
-->

Read `AGENTS.md` first. Then follow `.ai/prompts/review-change.md` step by step: identify which
skills from `.ai/skills/` apply to the change, load relevant `.ai/knowledge/` context, and run
`.ai/checklists/code-review.md` plus any other checklist the change touches (`.ai/checklists/security.md`,
`.ai/checklists/performance.md`, `.ai/checklists/data-change.md`, `.ai/checklists/plugin-release.md`)
before reporting the review complete.
