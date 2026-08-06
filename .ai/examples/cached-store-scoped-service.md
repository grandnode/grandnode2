# Example: Cached, Store-Scoped Business Service

Source: `src/Business/Grand.Business.Catalog/Services/Tax/TaxCategoryService.cs`

This is the canonical shape of a GrandNode business service. Almost every service in `Grand.Business.*` is a variation on it. Read it alongside `.ai/knowledge/caching.md`, `.ai/knowledge/scoping.md`, and `.ai/knowledge/domain-events.md`.

---

## Dependencies

```csharp
public class TaxCategoryService : ITaxCategoryService
{
    private readonly IRepository<TaxCategory> _taxCategoryRepository;
    private readonly IMediator _mediator;
    private readonly ICacheBase _cacheBase;

    public TaxCategoryService(ICacheBase cacheBase,
        IRepository<TaxCategory> taxCategoryRepository,
        IMediator mediator)
    {
        _cacheBase = cacheBase;
        _taxCategoryRepository = taxCategoryRepository;
        _mediator = mediator;
    }
}
```

Three dependencies, and they are the three a service of this kind almost always has: **repository abstraction**, **cache**, **mediator**. No `IMongoDatabase`, no `IServiceProvider`, no `IWorkContext` — the store id arrives as a parameter.

The class is registered `AddScoped` in the owning project's `IStartupApplication`.

## Read: cache wraps the query, store id is in the key

```csharp
public virtual async Task<IList<TaxCategory>> GetAllTaxCategories(string storeId = "")
{
    var key = string.Format(CacheKey.TAXCATEGORIES_ALL_KEY, storeId);
    return await _cacheBase.GetAsync(key, async () =>
    {
        var query = _taxCategoryRepository.Table.AsQueryable();
        if (!string.IsNullOrEmpty(storeId))
            query = query.Where(tc => tc.StoreId == storeId || string.IsNullOrEmpty(tc.StoreId));
        return await Task.FromResult(query.OrderBy(tc => tc.DisplayOrder).ToList());
    });
}
```

Four things at once:

1. **The key constant carries the parameter.** `TAXCATEGORIES_ALL_KEY` is `"Grand.taxcategory.all-{0}"`, documented in `CommonCacheKey.cs` with `{0} : store ID (empty = all stores)`.
2. **The store id is part of the key.** Omit it and store A serves store B's tax categories. This is a data leak, not a cache miss.
3. **`GetAsync` is read-through** — the delegate only runs on a miss. There is no separate `SetAsync`.
4. **Scoping is applied in the query**, not after materialization: records with an empty `StoreId` are global and visible to every store; the rest match the current store.

The by-id read has the same shape with a different parameter:

```csharp
public virtual Task<TaxCategory> GetTaxCategoryById(string taxCategoryId)
{
    var key = string.Format(CacheKey.TAXCATEGORIES_BY_ID_KEY, taxCategoryId);
    return _cacheBase.GetAsync(key, () => _taxCategoryRepository.GetByIdAsync(taxCategoryId));
}
```

Note it returns `Task<T>` directly without `async`/`await` — a pure pass-through, per `.ai/standards/csharp-style.md`.

## Write: guard, write, invalidate, publish — in that order

```csharp
public virtual async Task InsertTaxCategory(TaxCategory taxCategory)
{
    ArgumentNullException.ThrowIfNull(taxCategory);

    await _taxCategoryRepository.InsertAsync(taxCategory);

    await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

    //event notification
    await _mediator.EntityInserted(taxCategory);
}
```

The order is the contract:

1. `ArgumentNullException.ThrowIfNull` — the built-in helper, not a hand-written null check.
2. Repository write.
3. `RemoveByPrefix` with the `*_PATTERN_KEY` constant, which clears every key in the family regardless of which store ids happen to be cached.
4. `EntityInserted` — published **after** the write succeeded, through the `IMediator` extension rather than by constructing the notification.

`UpdateTaxCategory` and `DeleteTaxCategory` are identical in structure with `EntityUpdated` / `EntityDeleted`. All three invalidate. A service that caches on read but forgets to invalidate on delete serves deleted records until the entry expires.

## Cross-family invalidation

Delete does one extra thing:

```csharp
await _taxCategoryRepository.DeleteAsync(taxCategory);

//clear tax categories cache
await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);

//clear product cache
await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

//event notification
await _mediator.EntityDeleted(taxCategory);
```

Products embed a tax category, so cached product projections are stale the moment a tax category disappears. **When you add a cached family, ask what else embeds this data in a cached projection, and clear those prefixes too.** This is the step most often missed in review.

## `virtual` methods

Every public method is `virtual`. That is deliberate — plugins replace core services by registering their own implementation, and derived implementations override individual methods. Keep new service methods `virtual` for consistency with the surrounding code.

## What is deliberately absent

| Not here | Where it belongs |
|---|---|
| View models | `Grand.Web/Models` + a MediatR handler |
| `IWorkContext` | the caller — the service takes `storeId` explicitly |
| MongoDB filters and collections | inside `IRepository<T>` |
| Authorization checks | the controller's `[PermissionAuthorize]` attribute |
| Try/catch around the write | nothing here is an expected failure |

## Checklist when writing a service like this

- [ ] Constructor takes `IRepository<T>`, `ICacheBase`, `IMediator` — nothing infrastructural.
- [ ] Every cached read uses a `CacheKey` constant with every result-changing parameter formatted in.
- [ ] Store scope is applied inside the query.
- [ ] Every write invalidates with `RemoveByPrefix` and the `*_PATTERN_KEY` constant.
- [ ] Every write publishes the matching entity event, after the write.
- [ ] Cross-family caches that embed this entity are invalidated too.
- [ ] Guard clauses use `ArgumentNullException.ThrowIfNull`.
- [ ] Methods are `virtual`.
