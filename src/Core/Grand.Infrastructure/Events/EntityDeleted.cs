using Grand.Domain;
using MediatR;

namespace Grand.Infrastructure.Events;

/// <summary>
///     A container for passing entities that have been deleted.
/// </summary>
/// <typeparam name="T"></typeparam>
public class EntityDeleted<T> : INotification where T : ParentEntity

{
    public EntityDeleted(T entity)
    {
        Entity = entity;
    }

    public T Entity { get; private set; }
}