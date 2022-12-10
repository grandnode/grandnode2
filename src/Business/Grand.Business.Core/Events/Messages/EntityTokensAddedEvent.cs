using DotLiquid;
using Grand.Domain;
using Grand.Domain.Messages;
using MediatR;

namespace Grand.Business.Core.Events.Messages
{
    /// <summary>
    /// A container for tokens that are added.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class EntityTokensAddedEvent<T> : INotification where T : ParentEntity
    {
        private readonly T _entity;
        private readonly Drop _liquidDrop;
        private readonly LiquidObject _liquidObject;

        public EntityTokensAddedEvent(T entity, Drop liquidDrop, LiquidObject liquidObject)
        {
            _entity = entity;
            _liquidDrop = liquidDrop;
            _liquidObject = liquidObject;
        }

        public T Entity => _entity;
        public Drop LiquidDrop => _liquidDrop;
        public LiquidObject LiquidObject => _liquidObject;
    }
}
