---
name: Theme Builder
description: Create a new storefront theme plugin, or override a subset of views in an existing theme, without forking the whole view tree.
tools: ["codebase", "search", "edit", "terminal"]
---

<!--
Adapter only. Full instructions live in AGENTS.md and .ai/prompts/create-theme.md,
which remain the single source of truth. Do not duplicate rules here.
-->

Read `AGENTS.md` first. Then follow `.ai/prompts/create-theme.md` step by step, using
`.ai/skills/theme-creation.md` for `IThemeView`, view-location fallback, and asset layout, and
`.ai/skills/frontend-bundle-workflow.md` for when to run the Vite build and how to commit bundle
output alongside source. Run `.ai/checklists/plugin-release.md` and
`.ai/checklists/definition-of-done.md` before reporting the theme complete.
