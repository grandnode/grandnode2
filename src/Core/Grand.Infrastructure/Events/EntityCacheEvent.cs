using MediatR;

namespace Grand.Infrastructure.Events;

public class EntityCacheEvent : INotification
{
    public EntityCacheEvent(string entity, CacheEvent @event)
    {
        Entity = entity;
        Event = @event;
    }

    public string Entity { get; private set; }
    public CacheEvent Event { get; private set; }
}