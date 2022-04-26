using DotLiquid;
using Grand.Business.Core.Events.Messages;
using Grand.Domain;
using Grand.Domain.Messages;
using MediatR;

namespace Grand.Business.Core.Extensions
{
    public static class MessagesEventPublisherExtensions
    {
        public static async Task EntityTokensAdded<T>(this IMediator mediator, T entity, Drop liquidDrop, LiquidObject liquidObject) where T : ParentEntity
        {
            await mediator.Publish(new EntityTokensAddedEvent<T>(entity, liquidDrop, liquidObject));
        }

        public static async Task MessageTokensAdded(this IMediator mediator, MessageTemplate message, LiquidObject liquidObject)
        {
            await mediator.Publish(new MessageTokensAddedEvent(message, liquidObject));
        }
    }
}
