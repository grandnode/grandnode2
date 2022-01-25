using Grand.Infrastructure.Caching.Message;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Grand.Infrastructure.Caching.RabbitMq
{
    public class RabbitMqMessageCacheManager : MemoryCacheBase, ICacheBase
    {
        public static readonly string ClientId  = Guid.NewGuid().ToString("N");
        private readonly IMemoryCache _cache;
        private readonly IBus _bus;

        public RabbitMqMessageCacheManager(IMemoryCache cache, IMediator mediator, IBus bus)
            : base(cache, mediator)
        {
            _cache = cache;
            _bus = bus;
        }

        /// <summary>
        /// Removes the value with the specified key from the cache
        /// </summary>
        /// <param name="key">Key of cached item</param>
        public override async Task RemoveAsync(string key, bool publisher = true)
        {
            _cache.Remove(key);

            if (publisher)
                await _bus.Publish(new CacheMessageEvent() { ClientId = ClientId, Key = key, MessageType = (int)MessageEventType.RemoveKey });
        }

        /// <summary>
        /// Removes items by key prefix
        /// </summary>
        /// <param name="prefix">String prefix</param>
        /// <param name="publisher">publisher</param>
        public override async Task RemoveByPrefix(string prefix, bool publisher = true)
        {
            var entriesToRemove = _cacheEntries.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            foreach (var cacheEntrie in entriesToRemove)
            {
                _cache.Remove(cacheEntrie.Key);
            }

            if (publisher)
                await _bus.Publish(new CacheMessageEvent() { ClientId = ClientId, Key = prefix, MessageType = (int)MessageEventType.RemoveByPrefix });
        }

        ///<summary>
        /// Clear cache
        ///</summary>
        /// <param name="publisher">publisher</param>
        public override async Task Clear(bool publisher = true)
        {
            await base.Clear();
            if (publisher)
                await _bus.Publish(new CacheMessageEvent() { ClientId = ClientId, Key = "", MessageType = (int)MessageEventType.ClearCache });
        }
    }
}
