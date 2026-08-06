# Workflow: Refactor Safely

For changing structure without changing behavior.

**A refactor that changes behavior is not a refactor.** If behavior must change, that is a separate change, in a separate commit, described separately.

---

## Phase 1 — Justify

1. State the concrete problem: duplication in N places, a method nobody can follow, a boundary violation, a class doing three jobs.
2. State what becomes possible or safe afterwards.
3. Reject the refactor if the answer is only "it would be cleaner". Churn has a cost — review time, merge conflicts, and the risk that the "no behavior change" claim is wrong.
4. Check nobody is mid-change in the same files.

**Gate:** a stated problem, not a preference.

## Phase 2 — Establish the safety net

1. Find the existing tests covering the code. Run them; confirm they pass **before** you touch anything.
2. If coverage is thin, add characterization tests for current behavior first — including the behavior you suspect is wrong. Preserve it exactly; fixing it is a different change.
3. Identify every caller. Public interfaces, plugin extension points, and anything reachable from a view are contracts.
4. Note anything reachable from a plugin or theme — external code you cannot see may depend on it.

**Gate:** tests that would fail if the refactor breaks behavior. Without them, the refactor is unverifiable — say so and stop, or write the tests first.

## Phase 3 — Plan the sequence

1. Break it into steps that each leave the build green and the tests passing.
2. Order so that each step is independently revertible.
3. Prefer, in order: extract a method → extract a class → introduce an interface → move a type → change a signature.
4. Keep the old entry point delegating to the new one when callers are numerous; remove it in a later step.
5. Decide the boundary up front and write it down: which files are in scope. Everything else is out, however tempting.

**Gate:** a step list where every step is separately committable.

## Phase 4 — Execute

For each step:

1. Make the change.
2. Build.
3. Run the affected test project.
4. Commit.

Rules while executing:

- **No behavior changes.** Not a fixed edge case, not an added null check, not a corrected message, not a renamed resource key. Note them for later.
- **No opportunistic reformatting** outside the lines you are already changing — it hides the real diff.
- **No renamed persisted identity** — plugin system names, permission names, template names, task names, setting keys. Renaming a type is fine; renaming its persisted key is a data migration.
- If a step reveals a bug, stop. Note it, finish or revert the current step, then decide whether to fix it separately.

**Gate:** green build and green tests after every step, not only at the end.

## Phase 5 — Verify no behavior changed

- [ ] The same tests that passed in phase 2 still pass, unmodified.
- [ ] No assertion was weakened or deleted to make them pass.
- [ ] Public interfaces, view models, and routes are unchanged — or the PR declares a breaking change.
- [ ] Plugin extension points still resolve: providers, notification handlers, widget zones, view locations.
- [ ] DI lifetimes unchanged, unless the change was the point. A service that became a singleton while holding `IRepository<T>` is a live defect.
- [ ] Scope filters, cache keys, and invalidation prefixes survived the move intact — these are the details most often lost in a move.
- [ ] Localization keys unchanged.
- [ ] `.ai/checklists/code-review.md` run on the diff.

## Phase 6 — Report

- **Problem**: what was wrong with the previous structure.
- **Change**: the shape before and after.
- **Steps**: the commit sequence.
- **Behavior**: the explicit claim that none changed, and how it was verified.
- **Contracts**: any public interface, route, or persisted identity touched.
- **Deferred**: bugs and improvements found and deliberately not addressed.
