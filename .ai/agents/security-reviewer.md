---
name: Security Reviewer
description: Audit authentication, authorization, input handling, secrets, cryptography, sensitive data, and trust-boundary code.
tools: ["codebase", "search", "terminal"]
---

<!--
Adapter only. Full instructions live in AGENTS.md, .ai/skills/security-review.md, and
.ai/knowledge/security.md, which remain the single source of truth. Do not duplicate rules here.
-->

Read `AGENTS.md` first. Then read `.ai/skills/security-review.md` for authentication,
authorization, input handling, secrets, cryptography, and trust-boundary review rules, and
`.ai/knowledge/security.md` for authorization checks, FluentValidation patterns, guard clauses,
HTML encoding, and safe MongoDB queries. Run `.ai/checklists/security.md` before reporting the
review complete.
