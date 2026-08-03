# Best Practice: Architecture

Patterns from `Grand.Infrastructure`, `Grand.Business.*`, `Grand.Domain`. Complementary to `skills/reviews/architecture-review/SKILL.md`.

---

## Layering

```
Grand.Web / Grand.Module.Api   — HTTP, ViewModels, Controllers
Grand.Business.*               — Use cases, Services, Validators
Grand.Domain                   — Entities, Value objects (no dependencies)
Grand.Data / Grand.Infrastructure — DB access, Caching, Infrastructure
Grand.SharedKernel             — Interfaces and utilities shared across all layers
```

Rules:
- Domain has no dependencies on infrastructure or business layers.
- Business layer depends on Domain and Data abstractions (`IRepository<T>`), not on concrete Mongo types.
- Controllers delegate to MediatR — never contain business logic.
- Infrastructure registrations go in `IStartupApplication`, not `Program.cs`.

---

## Dependency Injection Registration

Implement `IStartupApplication` in each project that owns services. Split registration into private static `Register*` methods:

```csharp
public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterOrdersService(services);
        RegisterPaymentsService(services);
    }

    private static void RegisterOrdersService(IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderCalculationService, OrderCalculationService>();
    }

    private static void RegisterPaymentsService(IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
    }
}
```

### Lifetimes

| Lifetime | When to use |
|---|---|
| `AddScoped` | Business services, repositories — default choice |
| `AddSingleton` | Stateless infrastructure: `IMongoDatabase`, cache providers, `IAuditInfoProvider` |
| `AddTransient` | Rare; lightweight per-resolve objects with no shared state |
| `AddKeyedScoped` | Scheduled tasks (key must equal `ScheduleTask.ScheduleTaskName`) |

---

## MediatR — Commands and Queries

All mutations (writes) go through commands; all reads go through queries. Controllers only call `_mediator.Send(...)`.

### Command

```csharp
// in Grand.Business.Core/Commands/
public class MaxOrderNumberCommand : IRequest<int?>
{
    public int? OrderNumber { get; set; }
}

// in Grand.Business.Checkout/Commands/Handlers/
public class MaxOrderNumberCommandHandler : IRequestHandler<MaxOrderNumberCommand, int?>
{
    public async Task<int?> Handle(MaxOrderNumberCommand request, CancellationToken cancellationToken)
    {
        // write logic
    }
}
```

### Query

```csharp
// in Grand.Business.Core/Queries/
public class GetSuggestedProductsQuery : IRequest<IList<Product>>
{
    public string[] CustomerTagIds { get; set; }
    public int ProductsNumber { get; set; }
}
```

Command/query definitions belong in `Grand.Business.Core`; handlers belong in the relevant `Grand.Business.*` project.

---

## Domain Events

Publish domain events after every repository mutation so event handlers can react (cache clearing, side effects, integrations).

```csharp
await _repository.InsertAsync(entity);
await _mediator.EntityInserted(entity);

await _repository.UpdateAsync(entity);
await _mediator.EntityUpdated(entity);

await _repository.DeleteAsync(entity);
await _mediator.EntityDeleted(entity);
```

Event handlers implement `INotificationHandler<EntityInserted<T>>` (or Updated/Deleted). Place them in `Grand.Business.*/Events/Handlers/`.

---

## Anti-Patterns

| Anti-pattern | Correct |
|---|---|
| Business logic in a controller | Move to a command handler, call via `_mediator.Send` |
| Registering services in `Program.cs` | Register in `IStartupApplication.ConfigureServices` |
| Injecting `IMongoDatabase` into a business service | Inject `IRepository<T>` |
| Forgetting `_mediator.EntityUpdated` after `_repo.UpdateAsync` | Always publish after every mutation |
| Singleton service with `IRepository<T>` dependency | `IRepository<T>` is Scoped — its consumer must also be Scoped |
