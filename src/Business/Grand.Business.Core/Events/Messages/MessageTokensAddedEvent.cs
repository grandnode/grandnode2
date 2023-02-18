using Grand.Domain.Messages;
using MediatR;

namespace Grand.Business.Core.Events.Messages
{
    /// <summary>
    /// A container for tokens that are added.
    /// </summary>
    public class MessageTokensAddedEvent : INotification
    {
        private readonly MessageTemplate _message;
        private readonly LiquidObject _liquidObject;

        public MessageTokensAddedEvent(MessageTemplate message, LiquidObject liquidObject)
        {
            _message = message;
            _liquidObject = liquidObject;
        }

        public MessageTemplate Message => _message;
        public LiquidObject LiquidObject => _liquidObject;
    }
}
