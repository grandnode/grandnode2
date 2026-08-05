# Workflow: Fix a Bug

For a defect whose cause is not yet known. If the cause is already established and agreed, go straight to `.ai/prompts/implement-feature.md`.

**Do not write a fix before phase 2 is complete.** A change made during phase 1 is a guess.

---

## Phase 1 — Reproduce

Goal: turn a report into a deterministic failing case.

1. Restate the symptom in one sentence: what was expected, what happened.
2. Establish the conditions: which area (storefront / admin / store / vendor / API), which store, which customer group, which language and currency, which product type or order status, which plugins installed.
3. Reproduce it — as a failing test if the layer allows, otherwise as an exact click-path or request.
4. Determine the blast radius: one store or all? one customer or all? one product type or all? intermittent or deterministic?
5. Find when it started, if the report suggests a regression: `git log` the touched area.

**Gate:** you can make it fail on demand, and you can state the conditions under which it does *not* fail.

If you cannot reproduce it, stop and report what you tried and what information you need. Do not proceed on a hypothesis.

## Phase 2 — Find the cause

Goal: an explanation that accounts for **every** observation from phase 1, including the cases that work.

1. Trace the path: controller → MediatR request → handler → business service → cache → repository. See `.ai/knowledge/request-lifecycle.md`.
2. Check the usual causes before the unusual ones — in this codebase, in this order:
   - a cache key missing store id, language id, or another result-changing variable
   - a query missing a store or vendor filter
   - a write that does not invalidate its cache prefix
   - settings loaded without the store scope
   - `IWorkContext` read where there is no ambient context (scheduled task, migration, install)
   - a localized property read raw instead of through the working language
   - a notification handler throwing into a write path
   - a plugin overriding or shadowing core behavior
3. Confirm the cause by changing one thing and observing the failure move or disappear. Reading code is a hypothesis; making the symptom obey you is a confirmation.
4. Write the causal chain: *this input → this code path → this wrong state → this symptom.*

**Gate:** the explanation accounts for both the failing and the passing cases. If it explains only the failure, it is incomplete.

## Phase 3 — Choose a fix

Goal: pick deliberately, not by first idea.

1. Produce at least two candidates. Typically: the local fix at the symptom, and the fix at the cause.
2. For each, state scope, risk, and what else it touches.
3. Prefer the fix at the cause. Choose the local one only when the deeper fix is out of scope — and say so explicitly in the PR.
4. Check for siblings: if a cache key was missing a store id, look for the other keys in that family. Bugs of this kind come in sets.
5. Reject any fix that only suppresses the symptom — a swallowed exception, a defensive null check over an unexplained null, a cache clear on every request.

**Gate:** you can say why the chosen fix is right and why the alternative was rejected.

## Phase 4 — Assess risk

Before writing code, answer:

- What else calls this code?
- Which stores, vendors, product types, or payment flows are affected?
- Does existing data become invalid, or need a migration?
- Is any persisted identity or public contract touched? (Then it is a breaking change.)
- What happens on an installation that already worked around this bug?
- If the fix is wrong, how does an operator recover?

Run `.ai/checklists/data-change.md` if the answer touched entities, settings, or migrations.

**Gate:** no unanswered question above.

## Phase 5 — Fix, with a test first

1. Write the failing test first. Confirm it fails **for the reason from phase 2** — not for an unrelated reason. See `.ai/prompts/write-tests.md`.
2. Apply the minimal fix at the chosen level.
3. Confirm the test passes.
4. Fix the siblings found in phase 3 — in the same commit if they are the same defect, in a separate one if they are not.
5. Do not refactor surrounding code in the same change. Note it and move on.

**Gate:** a test exists that failed before and passes after.

## Phase 6 — Verify the impact

1. Run the affected test project.
2. Re-run the phase 1 reproduction.
3. Re-check the passing cases from phase 1 — the ones that worked before must still work.
4. Verify across the boundary the bug crossed: a second store, a second language, a different product type, a vendor as well as an admin.
5. Run `.ai/checklists/definition-of-done.md`.
6. Run `.ai/prompts/review-change.md` on your own diff.

## Report

- **Symptom**: what was observed.
- **Reproduction**: the exact conditions.
- **Cause**: the causal chain, with `path:line`.
- **Fix**: what changed and at which level; what was rejected and why.
- **Siblings**: other instances of the same defect, fixed or filed.
- **Test**: the regression test, and confirmation it failed before.
- **Risk**: what remains unverified.
- **Migration**: needed or not, and why.
