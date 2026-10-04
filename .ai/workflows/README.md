# Workflows

Multi-phase procedures with decision gates, for work where **the answer is not known when you start**.

## Workflows vs prompts

| | `.ai/prompts/` | `.ai/workflows/` |
|---|---|---|
| Goal | known — "build X" | known, but the **path** is not — "X is broken", "X is slow" |
| Shape | steps to produce a deliverable | investigate → decide → act → verify |
| Gates | none; you run it through | yes; each phase can send you back or stop the work |
| Example | `create-plugin.md` | `fix-bug.md` |

A workflow usually **calls** a prompt once it reaches the acting phase. `fix-bug.md` ends by handing off to `write-tests.md` and `review-change.md`.

Use a prompt when you already know what to build. Use a workflow when you first have to find out.

| Workflow | Situation |
|---|---|
| `fix-bug.md` | Something is broken and the cause is unknown |
| `investigate-performance.md` | Something is slow and the bottleneck is unknown |
| `refactor-safely.md` | Structure needs to change without behavior changing |
| `upgrade-dependency.md` | A package, framework, or shared version needs to move |
| `respond-to-review.md` | Review feedback arrived and has to be worked through |

## Rules for every workflow

1. **Finish a phase before starting the next.** The commonest failure is writing a fix before the cause is confirmed.
2. **A gate that fails sends you back**, not forward. Guessing past a gate produces changes that fix a symptom.
3. **State what you did not verify.** An unverified assumption carried into the next phase is where wrong fixes come from.
4. **Stop and ask** when a phase turns up something outside the stated scope — a second bug, a security issue, a design problem. Do not silently widen the work.
