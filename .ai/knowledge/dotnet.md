# Best Practice: C# / .NET Idioms

Patterns found across the GrandNode codebase. Covers C# language features and .NET conventions used here.

---

## Immutable Input Types — Records for Validators

Use positional records as input to FluentValidation validators. Records are immutable and their equality is structural:

```csharp
public record ShoppingCartStandardValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem);

public class ShoppingCartStandardValidator : AbstractValidator<ShoppingCartStandardValidatorRecord>
{ ... }
```

---

## Guard Clauses — ArgumentNullException

Use the built-in static helpers at the top of service methods. Do not write `if (x == null) throw new ArgumentNullException(...)` manually:

```csharp
public async Task SaveDeliveryDate(DeliveryDate deliveryDate)
{
    ArgumentNullException.ThrowIfNull(deliveryDate);
    // ...
}

public void Initialize(string keyId, string secretKey, string bucket)
{
    ArgumentNullException.ThrowIfNullOrEmpty(keyId);
    ArgumentNullException.ThrowIfNullOrEmpty(secretKey);
    ArgumentNullException.ThrowIfNullOrEmpty(bucket);
}
```

---

## Result Objects Instead of Exceptions for Expected Failures

For operations where failure is a valid business outcome, use a result object rather than throwing exceptions:

```csharp
public class PlaceOrderResult
{
    public bool Success => Errors.Count == 0;
    public IList<string> Errors { get; set; } = new List<string>();
    public Order PlacedOrder { get; set; }

    public void AddError(string error) => Errors.Add(error);
}
```

Exceptions are for unexpected, unrecoverable conditions. Validation failures, business rule violations, and "entity not found" are expected — return a result object.

---

## Pattern Matching

Use `switch` with `when` guards rather than nested `if/else`:

```csharp
switch (cartItem.ShoppingCartTypeId)
{
    case ShoppingCartType.ShoppingCart when product.DisableBuyButton:
        context.AddFailure(translationService.GetResource("ShoppingCart.BuyingDisabled"));
        break;
    case ShoppingCartType.Wishlist when product.DisableWishlistButton:
        context.AddFailure(translationService.GetResource("ShoppingCart.WishlistDisabled"));
        break;
}
```

---

## Nullable Reference Types

The project has nullable reference types enabled. Follow these conventions:

- `string` — the property/parameter is never null; calling code must not pass null.
- `string?` — explicitly nullable; caller or callee handles null.
- Do not suppress warnings with `!` unless you are certain the value cannot be null at that point.

---

## Configuration Binding

GrandNode uses direct `Bind` rather than `IOptions<T>`:

```csharp
var dbConfig = new DatabaseConfig();
configuration.GetSection("Database").Bind(dbConfig);
```

Do not introduce `IOptions<T>`, `IOptionsMonitor<T>`, or `IOptionsSnapshot<T>` — they are not used in this codebase and would be inconsistent.

---

## LINQ Expression Trees in Predicates

Repository methods accept `Expression<Func<T, bool>>`, not strings. Always use typed lambda predicates:

```csharp
// correct — type-safe, translates to a MongoDB query
var entity = await _repository.GetOneAsync(x => x.ExternalId == externalId);

// wrong — string-based, no compile-time safety
var entity = await collection.Find("{externalId: '" + externalId + "'}").FirstOrDefaultAsync();
```

---

## Naming Conventions

| Concept | Convention | Example |
|---|---|---|
| Private fields | `_camelCase` | `_orderRepository` |
| Constants | `UPPER_SNAKE_CASE` | `CacheKey.PRODUCTS_BY_ID` |
| Async methods | `Async` suffix | `GetByIdAsync` |
| Event handlers | `EntityDeletedEventHandler` | `ProductDeletedEventHandler` |
| Command | `*Command` | `PlaceOrderCommand` |
| Query | `*Query` | `GetSuggestedProductsQuery` |
| Validator input record | `*ValidatorRecord` | `ShoppingCartStandardValidatorRecord` |

---

## `using` Declaration for Disposables

Use the modern `using` declaration instead of a `try/finally` block with `Dispose`. The variable is disposed at the end of the enclosing scope:

```csharp
// correct — disposed when method exits
await using var response = await httpClient.GetStreamAsync(url);

// also correct for sync disposables
using var stream = File.OpenRead(path);

// old style — still valid but more verbose
using (var stream = File.OpenRead(path))
{
    // ...
}
```

The `await using` form is required for `IAsyncDisposable` (e.g., async streams).

## `var` Usage Conventions

Use `var` when the type is obvious from the right-hand side (constructor, cast, literal). Don't use it when the type is only readable from a method name:

```csharp
// clear — type is obvious
var product = new Product();
var products = await _productRepository.GetByIdAsync(id);

// prefer explicit — type is not obvious from method name alone
IList<Product> products = _catalogService.GetFeaturedProducts();
```

## Anti-Patterns

| Anti-pattern | Correct |
|---|---|
| `if (x == null) throw new ArgumentNullException(nameof(x))` | `ArgumentNullException.ThrowIfNull(x)` |
| Throwing `Exception` for a business rule violation | Return a result object with `.AddError(...)` |
| `IOptions<T>` for configuration | `configuration.GetSection(...).Bind(config)` |
| String-based MongoDB queries | LINQ expression predicates |
| `null!` suppression without a documented reason | Remove the null, or handle it |
| `try/finally { Dispose() }` | `using var x = ...` or `await using var x = ...` |
| `new HttpClient()` in a service | Inject `IHttpClientFactory`, call `CreateClient()` |
