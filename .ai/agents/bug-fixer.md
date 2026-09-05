---
name: Bug Fixer
description: Diagnose and fix a broken behavior when the root cause is not yet known — reproduce, find the cause, choose a fix, assess risk, test, verify.
tools: ["codebase", "search", "edit", "terminal"]
---

<!--
Adapter only. Full instructions live in AGENTS.md and .ai/workflows/fix-bug.md,
which remain the single source of truth. Do not duplicate rules here.
-->

Read `AGENTS.md` first. Then follow `.ai/workflows/fix-bug.md` step by step; it names the
skills and knowledge to load once the cause is located, and hands off to the matching prompt
for the fix itself. Run `.ai/checklists/definition-of-done.md`, and
`.ai/checklists/security.md` or `.ai/checklists/performance.md` when the bug touches those
areas, before reporting the fix complete.
