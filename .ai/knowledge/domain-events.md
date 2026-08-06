# Domain Events and Notifications

GrandNode uses MediatR for three distinct things. Keeping them apart matters, because they have different failure semantics.

| Kind | Interface | Handlers | Failure |
|---|---|---|---|
| Query | `IRequest<TResponse>` | exactly one | propagates to the caller |
| Command | `IRequest<TResponse>` | exactly one | propagates to the caller |
| Notification (event) | `INotification` | zero or many | a throwing handler breaks the publisher |

---

## Entity events

`src/Core/Grand.Infrastructure/Events/` defines the three lifecycle notifications:

```csharp
public class EntityInserted<T> : INotification where T : ParentEntity
{
    public EntityInserted(T entity) { Entity = entity; }
    public T Entity { get; private set; }
}
```

with `EntityUpdated<T>` and `EntityDeleted<T>` in the same shape.

Publish through the extensions in `Grand.Infrastructure/Extensions/EventPublisherExtensions.cs`:

```csharp
await _mediator.EntityInserted(taxCategory);
await _mediator.EntityUpdated(product);
await _mediator.EntityDeleted(category);
```

Do not construct the notification and call `Publish` by hand — use the extension.

### Where to publish

In the business service, after the repository write and after cache invalidation:

```csharp
await _taxCategoryRepository.InsertAsync(taxCategory);
await _cacheBase.RemoveByPrefix(CacheKey.TAXCATEGORIES_PATTERN_KEY);
await _mediator.EntityInserted(taxCategory);
```

Never publish an entity event from a controller, a handler, or a repository.

## Subscribing

Implement `INotificationHandler<T>`:

```csharp
public class ProductUpdatedHandler : INotificationHandler<EntityUpdated<Product>>
{
    public async Task Handle(EntityUpdated<Product> notification, CancellationToken cancellationToken)
    {
        // react
    }
}
```

Handlers are discovered by assembly scanning, including in plugins. This is the primary extension point for plugins that must react to core writes without modifying core.

### Handler rules

1. **A handler must not throw.** MediatR's default publisher runs handlers sequentially; an exception aborts the remaining handlers and surfaces in the caller's write path. Catch, log, and return.
2. A handler must be fast. It runs inline in the request. Long work belongs in a scheduled task — see `.ai/skills/scheduled-task.md`.
3. A handler must not assume ambient context. Entity events fire from scheduled tasks and migrations too, where `IWorkContext` is null. Read what you need off the entity, or take it as an explicit parameter. See `.ai/knowledge/scoping.md`.
4. A handler that writes the same entity type it subscribes to will re-enter itself. Guard it or restructure.
5. A handler is not a transaction participant — MongoDB writes are already committed when it runs. There is no rollback.

## Cache events

`Events/CacheEvent.cs` and `Events/EntityCacheEvent.cs` carry cache invalidation between application instances. Handlers for these pass `publisher: false` to `ICacheBase` so the invalidation is not rebroadcast. See `.ai/knowledge/caching.md`.

## Message and notification events

Plugins extend outbound messages by handling `MessageTokensAddedEvent` rather than editing the token provider. Message templates, DotLiquid drops, and the queued-email lifecycle are covered in `.ai/skills/message-notification.md`.

## Choosing between the three

| You want to | Use |
|---|---|
| read data for a view | query + handler in `Features/Handlers/` |
| perform a state change with a result the caller needs | command + handler in `Commands/Handler/` |
| let unknown code react to a state change | `INotification` published by the owning service |
| let a plugin extend core behavior without a fork | `INotificationHandler<T>` in the plugin |
| do slow or retryable work | `IScheduleTask`, triggered by a flag the handler sets |

## Anti-patterns

- Business logic in a notification handler that the write path actually depends on. If the write is wrong without it, it is not an event — put it in the service.
- Publishing an event before the write succeeds.
- A handler that queries back the entity it was just handed.
- Multiple handlers for the same event mutating the same entity, with ordering assumptions between them.
- Swallowing an exception in a handler with no logging.
