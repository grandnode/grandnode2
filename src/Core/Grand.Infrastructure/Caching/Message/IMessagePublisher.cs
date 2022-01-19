namespace Grand.Infrastructure.Caching.Message
{
    public interface IMessagePublisher
    {
        Task PublishAsync<TMessage>(TMessage msg) where TMessage : IMessageEvent;
    }
}
