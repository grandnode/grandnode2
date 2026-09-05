---
name: Performance Investigator
description: Diagnose slow behavior when the bottleneck is unknown — define, measure, explain, fix, re-measure.
tools: ["codebase", "search", "edit", "terminal"]
---

<!--
Adapter only. Full instructions live in AGENTS.md and .ai/workflows/investigate-performance.md,
which remain the single source of truth. Do not duplicate rules here.
-->

Read `AGENTS.md` first. Then follow `.ai/workflows/investigate-performance.md` step by step, using
`.ai/knowledge/performance.md` for `ICacheBase`, cache key constants, pagination, and partial
field write rules, and `.ai/knowledge/caching.md` for key composition and cross-family
invalidation. Run `.ai/checklists/performance.md` and `.ai/checklists/definition-of-done.md`
before reporting the fix complete.
