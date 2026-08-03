# Best Practice: Async / Await

Patterns derived from the GrandNode codebase. Every rule below has a real counterpart in production code.

---

## Rules

### Always use `async Task`, never `async void`

`async void` cannot be awaited; exceptions escape to `UnobservedTaskException`.

```csharp
// correct
public async Task HandleAsync(Command request, CancellationToken cancellationToken) { ... }

// never
public async void HandleAsync() { ... }
```

### Accept and forward CancellationToken throughout the call chain

Every MediatR handler already accepts `CancellationToken`. Pass it to every async call you make.

```csharp
public async Task<int?> Handle(MaxOrderNumberCommand request, CancellationToken cancellationToken)
{
    await _orderRepository.InsertAsync(new Order { ... }, cancellationToken);
}
```

If an API or repository method doesn't accept `CancellationToken`, you don't need to invent one — just don't create a new `CancellationToken.None` at the call site. Use the one provided.

### Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`

These block the thread and risk deadlocks under ASP.NET Core.

```csharp
// wrong — blocks thread
var product = _productService.GetProductByIdAsync(id).Result;

// correct
var product = await _productService.GetProductByIdAsync(id);
```

### Do NOT add `ConfigureAwait(false)`

ASP.NET Core has no synchronization context. `ConfigureAwait(false)` is noise here — the codebase does not use it and neither should you.

### Don't wrap I/O in `Task.Run`

`Task.Run` is for CPU-bound work. Database and network calls are already async at the driver level.

```csharp
// wrong — wastes a thread pool thread
var result = await Task.Run(() => _repository.GetByIdAsync(id));

// correct
var result = await _repository.GetByIdAsync(id);
```

### Use `ValueTask` only when the hot path frequently returns synchronously

`ValueTask` avoids allocation for code that often returns a cached result without awaiting. Do not retrofit existing `Task`-returning code unless profiling shows allocation is a problem.

### Avoid `.ToList()` inside async lambdas passed to cache

`IQueryable<T>.ToList()` is synchronous and blocks. Use LINQ's `ToList()` only on in-memory sequences, or use repository methods that return `Task<IList<T>>`.

```csharp
// This is fine — the LINQ query is materialised synchronously here
// because Table is IQueryable against MongoDB, not EF
var productIds = query.Take(request.ProductsNumber).ToList();
```

### Run independent operations concurrently with `Task.WhenAll`

When two or more async operations are independent, start all of them before awaiting:

```csharp
// GetProductOverviewHandler.cs — real codebase example
var tasks = new List<Task<ProductOverviewModel>>();
foreach (var product in request.Products)
    tasks.Add(GetProductOverviewModel(product, ...));

var result = await Task.WhenAll(tasks);
```

Don't `await` each item in a `foreach` loop if the iterations don't depend on each other — that forces sequential execution.

### LINQ + async: force evaluation before `Task.WhenAll`

LINQ uses deferred execution. If you build a task list with `Select(id => GetAsync(id))`, the tasks don't start until the sequence is iterated. Call `.ToArray()` or `.ToList()` immediately to start all tasks:

```csharp
// correct — all tasks start immediately
var tasks = ids.Select(id => GetProductByIdAsync(id)).ToArray();
var products = await Task.WhenAll(tasks);

// wrong — tasks created lazily, may not all start
var tasks = ids.Select(id => GetProductByIdAsync(id)); // no ToArray()
```

### `await Task.Delay` instead of `Thread.Sleep`

`Thread.Sleep` blocks the thread. `await Task.Delay` releases it back to the pool during the wait:

```csharp
// wrong
Thread.Sleep(1000);

// correct
await Task.Delay(1000);
```

---

## Blocking-to-Async Replacement Table

| Blocking (avoid) | Async equivalent |
|---|---|
| `task.Wait()` | `await task` |
| `task.Result` | `await task` |
| `Task.WaitAll(t1, t2)` | `await Task.WhenAll(t1, t2)` |
| `Task.WaitAny(t1, t2)` | `await Task.WhenAny(t1, t2)` |
| `Thread.Sleep(ms)` | `await Task.Delay(ms)` |

---

## Summary

| Rule | Reason |
|------|--------|
| `async Task` everywhere | `async void` exceptions are unobservable |
| Pass `CancellationToken` | Enables cooperative cancellation |
| No `.Result` / `.Wait()` | Deadlock risk |
| No `ConfigureAwait(false)` | No sync context in ASP.NET Core |
| No `Task.Run` for I/O | Wastes threads; I/O is already async |
