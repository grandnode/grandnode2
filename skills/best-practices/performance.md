# Best Practice: Performance

Patterns from `Grand.Infrastructure.Caching`, `Grand.Business.*`, and `Grand.Domain`.

---

## Caching

### Rule: every read-only service method that hits the DB must be cached

Wrap database calls with `ICacheBase.GetAsync`. Direct repository calls without a cache check are a performance bug.

```csharp
public virtual Task<PickupPoint> GetPickupPointById(string pickupPointId)
{
    var key = string.Format(CacheKey.PICKUPPOINTS_BY_ID_KEY, pickupPointId);
    return _cacheBase.GetAsync(key, () => _pickupPointsRepository.GetByIdAsync(pickupPointId));
}
```

### Define cache keys as `static string` in the `CacheKey` partial class

There are 21+ `CacheKey` partial classes under `Grand.Infrastructure/Caching/Constants/`. Add your keys there, not as inline strings.

```csharp
// in CacheKey partial for your domain area
public static string PICKUPPOINTS_BY_ID_KEY => "Grand.pickuppoint.id-{0}";
public static string PICKUPPOINTS_PATTERN_KEY => "Grand.pickuppoint.";
```

Use a dedicated pattern key for invalidation by prefix:

```csharp
await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);
```

### Invalidate after every write

Call `RemoveByPrefix` or `RemoveAsync` in Insert/Update/Delete service methods so the cache stays consistent.

```csharp
await _pickupPointsRepository.UpdateAsync(pickupPoint);
await _cacheBase.RemoveByPrefix(CacheKey.PICKUPPOINTS_PATTERN_KEY);
await _mediator.EntityUpdated(pickupPoint);
```

### Cache key format

```
"Grand.{entity}.{discriminator}-{param}"
```

Use `string.Format(CacheKey.MY_KEY, id)` at the call site, not string interpolation, to keep the key format centralised.

---

## Pagination

Never load an entire collection into memory. Use `PagedList<T>` with explicit page parameters.

```csharp
// admin list — large page size is acceptable
var list = await _service.GetAll(pageIndex: 0, pageSize: int.MaxValue);

// storefront — always constrain
var list = await _service.GetAll(pageIndex: pageIndex, pageSize: pageSize);
```

`PagedList<T>.Create(query, pageIndex, pageSize)` issues a `Count()` and a `Skip/Take` on the database, not in memory.

---

## Partial Writes

Replacing a full document when only one field changes wastes bandwidth and triggers index updates for every field. Use `UpdateField` for targeted writes:

```csharp
// instead of: entity.Published = true; await _repo.UpdateAsync(entity);
await _repository.UpdateField(id, x => x.Published, true);
```

---

## Projection

Fetch only the fields you need. On `Table` (IQueryable) this translates to a MongoDB projection:

```csharp
var productIds = _productRepository.Table
    .Where(p => p.Published)
    .Select(p => p.Id)       // projection — only Id is fetched
    .ToList();
```

Avoid pulling full documents just to read one property.

---

## HTTP Clients

Never `new HttpClient()` directly — use `IHttpClientFactory`. The push notifications service and exchange rate plugin are the canonical examples:

```csharp
// Startup registration
serviceCollection.AddHttpClient<IPushNotificationsService, PushNotificationsService>();

// Named client registration (plugin)
serviceCollection.AddHttpClient(Constant.DefaultHttpClientName);

// Consumption via IHttpClientFactory
public NbpExchange(IHttpClientFactory httpClientFactory)
{
    _httpClientFactory = httpClientFactory;
}

var client = _httpClientFactory.CreateClient(Constant.DefaultHttpClientName);
```

Instantiating `HttpClient` directly leaves sockets in `TIME_WAIT` and exhausts available connections under load.

## Exceptions Are Not Flow Control

Throwing an exception is expensive (stack walk, allocation). Use result objects for expected outcomes:

```csharp
// correct — expected failure as a result object
var result = new PlaceOrderResult();
result.AddError("Product is out of stock");
return result;

// avoid — throwing for an expected condition
throw new Exception("Product is out of stock");
```

This is enforced throughout the codebase by `PlaceOrderResult`, `CapturePaymentResult`, etc.

## Anti-Patterns

| Anti-pattern | Problem | Fix |
|---|---|---|
| Repository call without cache | DB hit on every request | Wrap in `ICacheBase.GetAsync` |
| `Table.ToList()` without filter | Full collection scan | Add `.Where(...)` first |
| Cache key as inline `$"key-{id}"` | Bypasses central key registry | Use `CacheKey.MY_KEY` constant |
| `int.MaxValue` page size on storefront | Returns entire collection to browser | Constrain to display page size |
| Full replace to update one field | Over-writes concurrent partial updates | Use `UpdateField<U>` |
| `new HttpClient()` directly | Socket exhaustion under load | Use `IHttpClientFactory` |
| `throw new Exception(...)` for a business rule | Expensive; hard to handle | Return a result object |
