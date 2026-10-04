---
name: Plugin Creator
description: Scaffold a new installable GrandNode plugin (payment, shipping, tax, widget, external authentication, discount rule, exchange rate, or theme) end to end.
tools: ["codebase", "search", "edit", "terminal"]
---

<!--
Adapter only. Full instructions live in AGENTS.md and .ai/prompts/create-plugin.md,
which remain the single source of truth. Do not duplicate rules here.
-->

Read `AGENTS.md` first. Then follow `.ai/prompts/create-plugin.md` step by step, using
`.ai/skills/plugin-module.md` (and the plugin-kind skill it points to, e.g.
`.ai/skills/plugin-payment.md`, `.ai/skills/plugin-shipping.md`, `.ai/skills/plugin-widget.md`,
`.ai/skills/plugin-discount-rules.md`) and `.ai/templates/plugin/base-plugin.md` as the scaffold.
Run `.ai/checklists/plugin-release.md` and `.ai/checklists/definition-of-done.md` before reporting
the plugin complete.
