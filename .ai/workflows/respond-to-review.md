# Workflow: Respond to Review Feedback

For working through review comments on a PR — human or automated.

The failure mode here is not laziness; it is **compliance without evaluation**. A reviewer can be wrong, and applying a wrong suggestion is worse than discussing it.

---

## Phase 1 — Collect and classify

1. Gather every comment, including ones already marked resolved.
2. Classify each:

   | Class | Meaning | Response |
   |---|---|---|
   | **Defect** | the code is wrong | fix |
   | **Risk** | not wrong, but could fail under conditions the reviewer names | verify, then fix or explain |
   | **Convention** | violates a rule in `.ai/standards/` or `.ai/constraints.md` | fix |
   | **Preference** | reviewer would have written it differently | discuss; adopt if the codebase agrees with them |
   | **Question** | reviewer does not understand something | answer; if the code caused the confusion, that is a finding |
   | **Out of scope** | valid, but not this PR | acknowledge, file separately |
   | **Incorrect** | reviewer is mistaken | say so, with evidence |

3. Order by severity, not by comment order.

**Gate:** every comment classified. None silently skipped.

## Phase 2 — Verify before agreeing

For each defect or risk comment:

1. Open the code and check the claim yourself.
2. Reproduce the failure the reviewer describes, if they describe one.
3. If you cannot reproduce it, say so and ask for the conditions — do not apply a speculative fix.
4. If the reviewer is right about a symptom but wrong about the cause, fix the cause and say what you found.

Automated reviewers produce confident, wrong findings at a measurable rate. Verify before acting on any of them.

**Gate:** you can state, for each accepted comment, what makes it correct.

## Phase 3 — Apply

1. Fix in severity order.
2. One concern per commit, so the reviewer can follow.
3. Do not bundle unrelated improvements into the response — that restarts the review.
4. If a comment reveals the same defect elsewhere in the diff, fix those too and say so.
5. If a fix is larger than the PR should carry, say that explicitly and propose a follow-up rather than half-doing it.

## Phase 4 — Push back where warranted

Disagreement is part of review. When you disagree:

- State the technical reason, not the effort involved.
- Cite the code, the standard, or the measurement.
- Offer the alternative you think is right.
- If the reviewer reaffirms after hearing the reason, that is their call — implement it, and note the trade-off in the thread rather than relitigating.

Never silently ignore a comment. An unanswered comment reads as an unfixed defect.

## Phase 5 — Re-verify

- [ ] The affected test project passes after the changes.
- [ ] Every accepted comment has a corresponding change.
- [ ] The fixes did not break anything the original PR got right.
- [ ] No unrelated change crept in.
- [ ] `.ai/checklists/definition-of-done.md` still passes for the whole PR, not just the delta.

## Phase 6 — Reply

For each comment, one line:

- **Fixed** — what changed, and where.
- **Fixed differently** — what you did instead, and why.
- **Not a defect** — the evidence.
- **Deferred** — why, and where it is tracked.
- **Answered** — the answer, plus any comment or rename added to prevent the same confusion.

Then a short summary: what changed since the last review, what was rejected and why, what is still open.
