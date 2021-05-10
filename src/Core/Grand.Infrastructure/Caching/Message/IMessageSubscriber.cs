using System.Threading.Tasks;

namespace Grand.Infrastructure.Caching.Message
{
    public interface IMessageSubscriber
    {
        Task SubscribeAsync();
    }
}
