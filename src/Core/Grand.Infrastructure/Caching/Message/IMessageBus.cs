using System.Threading.Tasks;

namespace Grand.Infrastructure.Caching.Message
{
    public interface IMessageBus : IMessagePublisher, IMessageSubscriber
    {
        Task OnSubscriptionChanged(IMessageEvent message);
    }
}
