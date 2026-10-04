# Best Practice: MongoDB / Repository

Patterns derived from `Grand.Data` and `Grand.Business.*`. Never bypass the repository abstraction.

---

## IRepository\<T\> — the only data access surface

Inject `IRepository<T>` where `T : BaseEntity`. Never inject `IMongoCollection<T>` or `IMongoDatabase` directly in business code.

```csharp
public class MyService
{
    private readonly IRepository<Product> _productRepository;
    
    public MyService(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }
}
```

---

## Query Patterns

### Fetch by ID

```csharp
var product = await _productRepository.GetByIdAsync(id);
```

### Fetch single by predicate

```csharp
var product = await _productRepository.GetOneAsync(p => p.Sku == sku);
```

### Fetch list with LINQ on `Table`

`Table` is `IQueryable<T>` backed by MongoDB driver; the query is translated and executed on the database, not in memory.

```csharp
var query = from cr in _customerTagProductRepository.Table
            where request.CustomerTagIds.Contains(cr.CustomerTagId)
            orderby cr.DisplayOrder
            select cr.ProductId;

var ids = query.Take(limit).ToList();
```

### Paginated list

```csharp
public async Task<IPagedList<PickupPoint>> GetAllPickupPoints(
    string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
{
    var query = _pickupPointsRepository.Table;
    if (!string.IsNullOrEmpty(storeId))
        query = query.Where(pp => pp.StoreId == storeId);
    query = query.OrderBy(pp => pp.DisplayOrder);
    return await PagedList<PickupPoint>.Create(query, pageIndex, pageSize);
}
```

---

## Write Patterns

### Insert

```csharp
await _repository.InsertAsync(entity);
```

After insert, publish the domain event:

```csharp
await _mediator.EntityInserted(entity);
```

### Full document replace (update all fields)

```csharp
await _repository.UpdateAsync(entity);
await _mediator.EntityUpdated(entity);
```

### Partial field update — prefer over full replace

Use `UpdateField` / `UpdateOneAsync` with `UpdateBuilder` when only one or a few fields change. This reduces write size and avoids overwriting concurrent changes.

```csharp
// update a single field
await _repository.UpdateField(id, x => x.DisplayOrder, newOrder);

// update multiple fields
await _repository.UpdateOneAsync(
    x => x.Id == id,
    new UpdateBuilder<Product>()
        .Set(x => x.Published, true)
        .Set(x => x.UpdatedOnUtc, DateTime.UtcNow));
```

### Delete

```csharp
await _repository.DeleteAsync(entity);
await _mediator.EntityDeleted(entity);
```

---

## Anti-Patterns

| Anti-pattern | Correct alternative |
|---|---|
| `_repository.Table.ToList()` on large collections | Add `.Where(...)` before `.ToList()`, or use `PagedList` |
| Fetching full document to update one field | `UpdateField<U>(id, x => x.Field, value)` |
| Fetching by ID then checking null inside a loop | Batch by IDs with `GetProductsByIds` style method |
| String-interpolated filter conditions | LINQ expression predicate — never string concatenation |
| Calling `_repository.Table.Count()` without filter | Can be expensive on large collections; filter first |

---

## Index Awareness

Queries on un-indexed fields cause collection scans. Before adding a query on a new field, check whether a MongoDB index exists in the migration files under `Grand.Module.Migration`. If not, add one.
