# Caching

`ICacheBase` (`src/Core/Grand.Infrastructure/Caching/ICacheBase.cs`) is the only caching abstraction. Backed by in-memory cache, or Redis when configured — the interface is the same either way, and the Redis backing is why invalidation has a `publisher` flag.

---

## The interface

```csharp
public interface ICacheBase
{
    T Get<T>(string key, Func<T> acquire);
    T Get<T>(string key, Func<T> acquire, int cacheTime);
    Task<T> GetAsync<T>(string key, Func<Task<T>> acquire);
    Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime);
    Task<T> SetAsync<T>(string key, Func<Task<T>> acquire);
    Task<T> SetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime);
    Task RemoveAsync(string key, bool publisher = true);
    Task RemoveByPrefix(string prefix, bool publisher = true);
    Task Clear(bool publisher = true);
}
```

`GetAsync` is read-through: the `acquire` delegate runs only on a miss. Never call the repository and then `SetAsync` separately.

`publisher: true` (the default) broadcasts the invalidation to other instances through the Redis message bus. Pass `false` **only** when handling an invalidation message that already arrived from another instance — otherwise you create an invalidation loop.

## Cache keys

Keys are `static string` members on the partial `CacheKey` class, split by area:

```
src/Core/Grand.Infrastructure/Caching/Constants/
  CommonCacheKey.cs, ProductCacheKey.cs, CategoryCacheKey.cs,
  CustomerCacheKey.cs, OrdersCacheKeys.cs, VendorCacheKey.cs, …
```

Two constants per cached family:

```csharp
/// <remarks>{0} : store ID (empty = all stores)</remarks>
public static string TAXCATEGORIES_ALL_KEY => "Grand.taxcategory.all-{0}";

/// Key pattern to clear cache
public static string TAXCATEGORIES_PATTERN_KEY => "Grand.taxcategory.";
```

Rules:

1. Prefix every key with `Grand.` and the entity family, lowercase, dot-separated.
2. The `*_PATTERN_KEY` is the shared prefix of every key in the family, and is what `RemoveByPrefix` takes.
3. Format parameters are documented in an XML `<remarks>` block, in order. Follow that convention — it is what makes the placeholders readable at the call site.
4. **Every variable that changes the result must be in the key.** Store id, language id, customer group set, currency, vendor id, page index. A missing store id in a key is a cross-store data leak, not a performance bug.
5. Never build a key inline as a string literal at the call site. Add a constant.

## Usage

Read path, in the business service:

```csharp
public virtual async Task<IList<TaxCategory>> GetAllTaxCategories(string storeId = "")
{
    var key = string.Format(CacheKey.TAXCATEGORIES_ALL_KEY, storeId);
    return await _cacheBase.GetAsync(key, async () =>
    {
        var query = _taxCategoryRepository.Table.AsQueryable();
        // …
    });
}
```

Write path, in the same service:

```csharp
await _taxCategoryRepository.InsertAsync(taxCategory);

await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

//event notification
await _mediator.EntityInserted(taxCategory);
```

Invalidate on **every** write — insert, update, and delete. A service that caches on read but forgets to invalidate on delete serves deleted records until the entry expires.

## Cross-family invalidation

A write sometimes invalidates families it does not own. `TaxCategoryService.Delete` clears both its own prefix and `CacheKey.PRODUCTS_PATTERN_KEY`, because products carry a tax category.

When adding a cached family, ask what *else* embeds this data in a cached projection, and clear those prefixes too. When adding a new relationship, revisit the invalidation of both sides.

## Where caching belongs

| Layer | Caches? |
|---|---|
| Controller | No |
| Mediator handler | No — a handler may compose cached services, but does not own a cache key |
| Business service | **Yes** — this is the only layer that calls `ICacheBase` |
| Repository | No |

## Anti-patterns

- A cache key without the store id for store-scoped data.
- A cache key without the language id for localized data.
- `RemoveByPrefix` with a hand-written string instead of the `*_PATTERN_KEY` constant.
- `Clear()` to fix a stale entry — it evicts everything for every store.
- Caching an entity graph that contains customer-specific data under a key that omits the customer.
- `publisher: false` outside an invalidation-message handler.
- Caching in a scheduled task without a key that includes the store it is processing.

See also `.ai/knowledge/performance.md` for pagination and partial-write guidance, and `.ai/knowledge/domain-events.md` for the event that accompanies each invalidation.
