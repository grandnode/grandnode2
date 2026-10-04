using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.Extensions.Caching.Memory;

namespace Grand.Infrastructure.Caching.Redis;

public class RedisMessageCacheManager : MemoryCacheBase, ICacheBase
{
    private readonly IMessageBus _messageBus;

    public RedisMessageCacheManager(IMemoryCache cache, IMediator mediator, IMessageBus messageBus, CacheConfig config)
        : base(cache, mediator, config)
    {
        _messageBus = messageBus;
    }

    /// <summary>
    ///     Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="publisher">Publisher</param>
    public override async Task RemoveAsync(string key, bool publisher = true)
    {
        await base.RemoveAsync(key, false);

        if (publisher)
            await _messageBus.PublishAsync(new MessageEvent
                { Key = key, MessageType = (int)MessageEventType.RemoveKey });
    }

    /// <summary>
    ///     Removes items by key prefix
    /// </summary>
    /// <param name="prefix">String prefix</param>
    /// <param name="publisher">publisher</param>
    public override async Task RemoveByPrefix(string prefix, bool publisher = true)
    {
        await base.RemoveByPrefix(prefix, false);

        if (publisher)
            await _messageBus.PublishAsync(new MessageEvent
                { Key = prefix, MessageType = (int)MessageEventType.RemoveByPrefix });
    }

    /// <summary>
    ///     Clear cache
    /// </summary>
    /// <param name="publisher">publisher</param>
    public override async Task Clear(bool publisher = true)
    {
        await base.Clear(publisher);

        if (publisher)
            await _messageBus.PublishAsync(new MessageEvent
                { Key = "", MessageType = (int)MessageEventType.ClearCache });
    }
}
