using DotLiquid;
using Grand.Domain;
using Grand.Domain.Messages;
using MediatR;

namespace Grand.Business.Core.Events.Messages;

/// <summary>
///     A container for tokens that are added.
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class EntityTokensAddedEvent<T> : INotification where T : ParentEntity
{
    public EntityTokensAddedEvent(T entity, Drop liquidDrop, LiquidObject liquidObject)
    {
        Entity = entity;
        LiquidDrop = liquidDrop;
        LiquidObject = liquidObject;
    }

    public T Entity { get; }

    public Drop LiquidDrop { get; }

    public LiquidObject LiquidObject { get; }
}