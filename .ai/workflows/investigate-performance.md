# Workflow: Investigate Performance

For "X is slow" when the bottleneck is unknown.

**Do not optimize before phase 3.** An unmeasured optimization that costs readability is a net loss — see `.ai/principles.md`.

---

## Phase 1 — Define the problem

1. What exactly is slow: a page, an admin grid, a scheduled task, an API endpoint, the whole site?
2. How slow, and compared to what: a number, or "it used to be fast" with a version.
3. Under what conditions: catalog size, order volume, number of stores, concurrent users, cold or warm cache.
4. Is it constant, or does it degrade with data volume? Degrading with volume points at a query; constant points at a per-request cost.
5. Is it one store or all? One instance or all?

**Gate:** you have a specific, repeatable slow operation, not a general complaint.

## Phase 2 — Measure

1. Reproduce with a realistic dataset. Ten products prove nothing; the problems appear at tens of thousands.
2. Measure the whole operation before measuring parts.
3. Narrow by layer, in this order — the answer is usually found before the last:
   - **Query count.** Is a query running inside a loop? This is the most common cause by a wide margin.
   - **Query shape.** Unbounded result set, missing index, filtering or paging after materialization.
   - **Cache.** Is the path cached at all? Is the key so specific it never hits? Is something clearing the prefix on every request?
   - **Render.** A view component invoked per item, a service call from a view, view-model preparation inside a loop.
   - **External calls.** A blocking HTTP call on the render path.
   - **Allocation.** Large collections copied repeatedly, string building in a loop.
4. Compare warm-cache against cold-cache timings — a large gap means the problem is upstream of the cache; no gap means the cache is not working.

**Gate:** you can name the specific operation consuming the time, with a number. "Probably the database" is not a measurement.

## Phase 3 — Explain before fixing

1. State why it is slow, mechanically: *N products → N queries → N round trips.*
2. Predict the improvement the fix should produce.
3. Check the cause is not a correctness bug in disguise — a cache key missing a store id both leaks data and destroys the hit rate. If so, switch to `fix-bug.md`; the leak is the more serious finding.

**Gate:** the explanation predicts the measurement you already took.

## Phase 4 — Choose the fix

In order of preference:

1. **Remove the work.** Do not query what is not displayed. Do not compute what is not used.
2. **Batch it.** One query for the set instead of one per item.
3. **Push it into the query.** Filter, sort, and page in the database.
4. **Cache it** — if it is read far more than written, and the key can carry every result-changing variable. See `.ai/knowledge/caching.md`. A cache is a commitment to invalidate correctly, not a free win.
5. **Add an index** to support the query shape.
6. **Move it out of the request** to a scheduled task.
7. **Micro-optimize** — last, and only with a measurement.

Reject any fix that trades correctness for speed: dropping a scope filter, caching under a key that omits the store, or serving stale data by skipping invalidation.

**Gate:** the fix does not weaken any boundary in `.ai/knowledge/scoping.md`.

## Phase 5 — Apply and re-measure

1. Apply one change at a time.
2. Re-measure after each. A change that does not move the number gets reverted, not kept "because it should help".
3. If caching was added: verify invalidation on **every** write path including delete, and verify the key contains store id and language id where relevant.
4. Verify correctness has not moved: same output, same data, across two stores and two languages.
5. Run `.ai/checklists/performance.md` and `.ai/checklists/definition-of-done.md`.

**Gate:** before and after numbers, from the same dataset.

## Phase 6 — Report

- **Operation**: what was measured.
- **Dataset**: size and shape — the numbers are meaningless without it.
- **Before / after**: measured, not estimated.
- **Cause**: the mechanical explanation.
- **Fix**: what changed, and which alternatives were rejected.
- **Correctness**: how you verified output is unchanged, and across which boundaries.
- **Invalidation**: if caching was added, every write path that clears it.
- **Remaining**: what is still slow, and what was not measured.
