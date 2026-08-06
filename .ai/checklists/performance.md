# Checklist: Performance

Run when the change adds a query, iterates over entities, touches a page-render path, or changes caching.

Complementary to `.ai/knowledge/performance.md` and `.ai/knowledge/caching.md`.

---

## Queries

- [ ] No query inside a loop. Fetch the set once and join in memory, or push the filter into the query.
- [ ] Filtering, sorting, and paging happen in the query — not after `ToList()`.
- [ ] The query projects the fields it needs when the entity is large and only a few are used.
- [ ] List endpoints are paged. An unbounded list over a growing collection is a future outage.
- [ ] Existence checks use a count or an any-style query, not "load everything and check `Count`".
- [ ] New query shapes are supported by an index, or the absence of one is stated deliberately.

## Writes

- [ ] Updating a few fields uses a partial update rather than rewriting the whole document.
- [ ] Bulk operations are batched rather than issued one document at a time.
- [ ] No read-modify-write loop that could be a single update.

## Caching

- [ ] Data that is read far more than written, and is expensive to produce, is cached.
- [ ] The cache key contains every variable that changes the result — store, language, currency, customer group, vendor, page.
- [ ] Cached values are invalidated on **every** write path, including delete.
- [ ] Cross-family caches that embed this data are invalidated too.
- [ ] Nothing customer-specific is cached under a key that omits the customer.
- [ ] `Clear()` is not used to fix a stale entry — it evicts every store's cache.
- [ ] Caching sits in the business service, not in a controller or handler.

## Render path

- [ ] No repository or service call from a Razor view.
- [ ] A view component that loads data is not invoked inside a loop over products.
- [ ] View-model preparation happens once in the handler, not per item.
- [ ] Images carry `loading` and dimension attributes as the surrounding views do.
- [ ] No new blocking external HTTP call on a page-render path. If one is unavoidable, it has a timeout and a fallback.

## Async and concurrency

- [ ] Nothing blocks on a `Task`.
- [ ] Independent awaits that could run concurrently are not serialized in a loop when the underlying calls are safe to parallelize.
- [ ] Long work is moved to a scheduled task rather than run inside a request.
- [ ] Notification handlers are fast — they run inline in the write path.

## Allocation

- [ ] No repeated string concatenation in a loop where a builder is available.
- [ ] Large collections are not copied repeatedly between list types.
- [ ] Nothing large is held in a singleton or static field.

## Scale assumptions

- [ ] The change was reasoned about with a realistic catalog: tens of thousands of products, not ten.
- [ ] Behavior under multiple stores considered — per-store caching multiplies memory.
- [ ] Behavior under multiple application instances considered — cache invalidation must propagate.

## Evidence

- [ ] Any optimization that costs readability is justified by a measurement, not intuition.
- [ ] The measurement, or its absence, is stated in the PR.
- [ ] Nothing was optimized speculatively at the expense of clarity.
