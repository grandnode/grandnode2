using Grand.Infrastructure.Caching.Message;
using Grand.Infrastructure.Configuration;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace Grand.Infrastructure.Caching.RabbitMq;

public class RabbitMqMessageCacheManager : MemoryCacheBase, ICacheBase
{
    public static readonly string ManageClientId = Guid.NewGuid().ToString("N");
    private readonly IBus _bus;
    private readonly IMemoryCache _cache;

    public RabbitMqMessageCacheManager(IMemoryCache cache, IMediator mediator, IBus bus, CacheConfig config)
        : base(cache, mediator, config)
    {
        _cache = cache;
        _bus = bus;
    }

    /// <summary>
    ///     Removes the value with the specified key from the cache
    /// </summary>
    /// <param name="key">Key of cached item</param>
    /// <param name="publisher">Publisher</param>
    public override async Task RemoveAsync(string key, bool publisher = true)
    {
        _cache.Remove(key);

        if (publisher)
            await _bus.Publish(new CacheMessageEvent
                { ClientId = ManageClientId, Key = key, MessageType = (int)MessageEventType.RemoveKey });
    }

    /// <summary>
    ///     Removes items by key prefix
    /// </summary>
    /// <param name="prefix">String prefix</param>
    /// <param name="publisher">publisher</param>
    public override async Task RemoveByPrefix(string prefix, bool publisher = true)
    {
        var entriesToRemove = CacheEntries.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        foreach (var cacheEntries in entriesToRemove) _cache.Remove(cacheEntries.Key);

        if (publisher)
            await _bus.Publish(new CacheMessageEvent
                { ClientId = ManageClientId, Key = prefix, MessageType = (int)MessageEventType.RemoveByPrefix });
    }

    /// <summary>
    ///     Clear cache
    /// </summary>
    /// <param name="publisher">publisher</param>
    public override async Task Clear(bool publisher = true)
    {
        await base.Clear();
        if (publisher)
            await _bus.Publish(new CacheMessageEvent
                { ClientId = ManageClientId, Key = "", MessageType = (int)MessageEventType.ClearCache });
    }
}