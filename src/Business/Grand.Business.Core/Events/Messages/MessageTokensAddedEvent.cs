using Grand.Domain.Messages;
using MediatR;

namespace Grand.Business.Core.Events.Messages;

/// <summary>
///     A container for tokens that are added.
/// </summary>
public class MessageTokensAddedEvent : INotification
{
    public MessageTokensAddedEvent(MessageTemplate message, LiquidObject liquidObject)
    {
        Message = message;
        LiquidObject = liquidObject;
    }

    public MessageTemplate Message { get; }

    public LiquidObject LiquidObject { get; }
}