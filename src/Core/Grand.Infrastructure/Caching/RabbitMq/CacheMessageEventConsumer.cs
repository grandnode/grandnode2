using Grand.Infrastructure.Caching.Message;
using MassTransit;
using System.Threading.Tasks;

namespace Grand.Infrastructure.Caching.RabbitMq
{
    public class CacheMessageEventConsumer : IConsumer<CacheMessageEvent>
    {
        private readonly ICacheBase _cache;

        public CacheMessageEventConsumer(ICacheBase cache)
        {
            _cache = cache;
        }

        public async Task Consume(ConsumeContext<CacheMessageEvent> context)
        {
            var message = context.Message;
            if (RabbitMqMessageCacheManager.ClientId.Equals(message.ClientId)) return;
            switch (message.MessageType)
            {
                case (int)MessageEventType.RemoveKey:
                    await _cache.RemoveAsync(message.Key, false);
                    break;
                case (int)MessageEventType.RemoveByPrefix:
                    await _cache.RemoveByPrefix(message.Key, false);
                    break;
                case (int)MessageEventType.ClearCache:
                    await _cache.Clear(false);
                    break;
            }
        }
    }
}
