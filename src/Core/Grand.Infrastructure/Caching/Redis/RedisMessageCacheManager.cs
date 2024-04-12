using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Configuration;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Grand.Infrastructure.Caching.Redis;

public class RedisMessageCacheManager : MemoryCacheBase, ICacheBase
{
    private readonly IMemoryCache _cache;
    private readonly IMessageBus _messageBus;

    public RedisMessageCacheManager(IMemoryCache cache, IMediator mediator, IMessageBus messageBus, CacheConfig config)
        : base(cache, mediator, config)
    {
        _cache = cache;
        _messageBus = messageBus;
    }

    /// <summary>
    ///     Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="publisher">Publisher</param>
    public override Task RemoveAsync(string key, bool publisher = true)
    {
        _cache.Remove(key);

        if (publisher)
            _messageBus.PublishAsync(new MessageEvent { Key = key, MessageType = (int)MessageEventType.RemoveKey });

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Removes items by key prefix
    /// </summary>
    /// <param name="prefix">String prefix</param>
    /// <param name="publisher">publisher</param>
    public override Task RemoveByPrefix(string prefix, bool publisher = true)
    {
        var entriesToRemove = CacheEntries.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        foreach (var cacheEntries in entriesToRemove) _cache.Remove(cacheEntries.Key);

        if (publisher)
            _messageBus.PublishAsync(new MessageEvent
                { Key = prefix, MessageType = (int)MessageEventType.RemoveByPrefix });

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Clear cache
    /// </summary>
    /// <param name="publisher">publisher</param>
    public override Task Clear(bool publisher = true)
    {
        base.Clear();
        if (publisher)
            _messageBus.PublishAsync(new MessageEvent { Key = "", MessageType = (int)MessageEventType.ClearCache });

        return Task.CompletedTask;
    }
}