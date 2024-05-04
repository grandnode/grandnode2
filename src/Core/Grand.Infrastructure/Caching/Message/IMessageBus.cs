namespace Grand.Infrastructure.Caching.Message;

public interface IMessageBus : IMessagePublisher, IMessageSubscriber
{
    void OnSubscriptionChanged(IMessageEvent message);
}