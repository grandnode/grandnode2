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
4. **Every variable that changes the result must be in the key.** Store id, language id, customer group set, currency, vendor id, page index. A missing store id in a key is a cross-store data leak, not a performance bug. Format the customer group set of data the ACL filters with `_accessControlConfig.GetCustomerGroupsCacheKey(customer)`, or `access.GetCustomerGroupsCacheKey(_accessControlConfig)` when the read takes a `CatalogAccess`: both are empty when `IgnoreAcl` is on (and the second for reads that include hidden records), since the groups filter nothing then. Data limited to groups by something other than the ACL keeps the groups: the widget list (`IsAuthenticateGroup` on providers) uses `customer.GetCustomerGroupsCacheKey()`, and customer group recommendations are keyed by the groups themselves. Never join the group ids by hand.
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

Write path: the service writes and publishes the entity event, nothing else:

```csharp
await _taxCategoryRepository.InsertAsync(taxCategory);

//event notification
await _mediator.EntityInserted(taxCategory);
```

The family's cache event handler clears the keys:

```csharp
public class TaxCategoryCacheEventHandler :
    INotificationHandler<EntityInserted<TaxCategory>>,
    INotificationHandler<EntityUpdated<TaxCategory>>,
    INotificationHandler<EntityDeleted<TaxCategory>>
{
    // …
    public async Task Handle(EntityDeleted<TaxCategory> notification, CancellationToken cancellationToken)
    {
        await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);
    }
}
```

One `<Family>CacheEventHandler` per cached family, in the project of the service that owns it (`Events/Handlers/Cache` in Catalog, `Events/Cache` elsewhere), handles insert, update and delete. Every write must publish its event, or nothing is cleared: a service that caches on read but writes without an event serves stale records until the entry expires. A write that had no event gets one (discount coupons, permissions, entity URLs got theirs this way).

Records embedded in a product (tier prices, pictures, category mappings, attributes, …) publish `EntityX<T>` of the embedded record, which does not carry the product id. Every write of an existing product therefore also publishes `ProductChangedEvent(productId)`, at the point where it used to remove the product's cache entry. `ProductCacheEventHandler` clears that product, and with `listsChanged: true` also the product lists (listings, home page, customer group, tag and personal lists). It does not handle `EntityUpdated<Product>`: stock and auction writes publish that event on every order, and must not clear the lists.

Handlers run one after another and stop at the first exception (`Grand.Mediator`), in registration order. A handler that fails before the cache handler leaves the cache stale, and a handler of the same event that reads the cached family may still see the old entry. Keep handlers that react to an entity write free of reads of that entity's cached family.

What still clears the cache directly, on purpose: the `ICacheBase` implementations and the Redis bus, migrations (they write through repositories without events), `ISettingService.ClearCache` (a public API), the read-path eviction of an unknown slug in `SlugService.GetBySlugCached`, and `OidcSessionService` (a session store with conditional updates and no entity to publish).

## Cross-family invalidation

A write sometimes invalidates families it does not own. A deleted brand, category, collection, tax category or measure unit is also removed from the products that carry it, so `CacheKey.PRODUCTS_PATTERN_KEY` is cleared too.

Clear the other family where its records are rewritten, after the write. When a `<Entity>DeletedEventHandler` rewrites the products, it clears the product prefix itself, as `CategoryDeletedEventHandler` does: the family's cache event handler handles the same event, may run before the rewrite, and a request in between would cache the old products again. When the service rewrites them, it publishes `ProductChangedEvent` for each product it changed, as `ProductTagService.DeleteProductTag` does.

When adding a cached family, ask what *else* embeds this data in a cached projection, and clear those prefixes too: in its cache event handler when nothing rewrites the embedded copies, after the rewrite when something does. When adding a new relationship, revisit the handlers of both sides.

## Where caching belongs

| Layer | Caches? |
|---|---|
| Controller | No |
| Mediator handler | Reads: no — a handler may compose cached services, but does not own a cache key. Invalidation: the family's `<Family>CacheEventHandler`, and a handler that rewrites another family's records clears that family after its write |
| Business service | **Yes**, reads — the read-through `GetAsync` lives here; writes publish events instead of clearing |
| Repository | No |

## Anti-patterns

- A cache key without the store id for store-scoped data.
- A cache key without the language id for localized data.
- `RemoveByPrefix` with a hand-written string instead of the `*_PATTERN_KEY` constant.
- `RemoveByPrefix` in a service after a write instead of in the family's cache event handler.
- `string.Join(",", customer.GetCustomerGroupIds())` in a key instead of `GetCustomerGroupsCacheKey`.
- `Clear()` to fix a stale entry — it evicts everything for every store.
- Caching an entity graph that contains customer-specific data under a key that omits the customer.
- `publisher: false` outside an invalidation-message handler.
- Caching in a scheduled task without a key that includes the store it is processing.

See also `.ai/knowledge/performance.md` for pagination and partial-write guidance, and `.ai/knowledge/domain-events.md` for the event that accompanies each invalidation.
