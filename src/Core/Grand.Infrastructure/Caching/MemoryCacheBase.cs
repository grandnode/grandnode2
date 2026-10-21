using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace Grand.Infrastructure.Caching;

/// <summary>
///     Represents a manager for memory caching
/// </summary>
public class MemoryCacheBase : ICacheBase, IDisposable
{
    #region Ctor

    public MemoryCacheBase(IMemoryCache cache, IMediator mediator, CacheConfig cacheConfig)
    {
        _cache = cache;
        _mediator = mediator;
        _cacheConfig = cacheConfig;
    }

    #endregion

    #region Fields

    private readonly IMemoryCache _cache;
    private readonly IMediator _mediator;
    private readonly CacheConfig _cacheConfig;

    private CancellationTokenSource _resetCacheToken = new();

    protected readonly ConcurrentDictionary<string, SemaphoreSlim> CacheEntries = new();

    #endregion

    #region Methods

    public virtual bool TryGet<T>(string key, out T value)
    {
        return _cache.TryGetValue(key, out value);
    }

    public virtual T Get<T>(string key, Func<T> acquire)
    {
        return Get(key, acquire, _cacheConfig.DefaultCacheTimeMinutes);
    }

    public virtual T Get<T>(string key, Func<T> acquire, int cacheTime)
    {
        if (_cache.TryGetValue(key, out T cacheEntry)) return cacheEntry;
        var semaphore = CacheEntries.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        semaphore.Wait();
        try
        {
            if (!_cache.TryGetValue(key, out cacheEntry))
            {
                cacheEntry = acquire();
                SetEntry(key, cacheEntry, cacheTime);
            }
        }
        finally
        {
            semaphore.Release();
        }

        return cacheEntry;
    }

    public virtual Task<T> GetAsync<T>(string key, Func<Task<T>> acquire)
    {
        return GetAsync(key, acquire, _cacheConfig.DefaultCacheTimeMinutes);
    }

    public virtual async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime)
    {
        if (_cache.TryGetValue(key, out T cacheEntry)) return cacheEntry;
        var semaphore = CacheEntries.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            if (!_cache.TryGetValue(key, out cacheEntry))
            {
                cacheEntry = await acquire();
                SetEntry(key, cacheEntry, cacheTime);
            }
        }
        finally
        {
            semaphore.Release();
        }

        return cacheEntry;
    }

    public virtual Task<T> SetAsync<T>(string key, Func<Task<T>> acquire)
    {
        return SetAsync(key, acquire, _cacheConfig.DefaultCacheTimeMinutes);
    }

    public virtual async Task<T> SetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime)
    {
        var semaphore = CacheEntries.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            var cacheEntry = await acquire();
            SetEntry(key, cacheEntry, cacheTime);
            return cacheEntry;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public virtual async Task RemoveAsync(string key, bool publisher = true)
    {
        _cache.Remove(key);

        if (publisher)
            await _mediator.Publish(new EntityCacheEvent(key, CacheEvent.RemoveKey));
    }

    public virtual async Task RemoveByPrefix(string prefix, bool publisher = true)
    {
        var entriesToRemove = CacheEntries.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        foreach (var cacheEntries in entriesToRemove) _cache.Remove(cacheEntries.Key);

        if (publisher)
            await _mediator.Publish(new EntityCacheEvent(prefix, CacheEvent.RemovePrefix));
    }

    public virtual Task Clear(bool publisher = true)
    {
        //clear keys
        foreach (var cacheEntry in CacheEntries.Keys.ToList())
            _cache.Remove(cacheEntry);

        //cancel, but do not dispose: a writer that already read this source is still handing it to
        //MemoryCache, which registers an eviction callback on it while storing the entry, and that
        //registration throws ObjectDisposedException on a disposed source. Cancelling releases the
        //registrations, and the source has no timer and no WaitHandle, so it is simply collected
        _resetCacheToken.Cancel();

        _resetCacheToken = new CancellationTokenSource();

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Disposes the current reset token once the cache manager itself is no longer needed
    ///     (e.g. on application shutdown). Unlike <see cref="Clear" />, there is no concurrent writer
    ///     activity at this point, so disposing here does not risk an ObjectDisposedException.
    ///     Safe to call more than once.
    /// </summary>
    public void Dispose()
    {
        if (_resetCacheToken.IsCancellationRequested) return;

        _resetCacheToken.Cancel();
        _resetCacheToken.Dispose();
    }

    /// <summary>
    ///     Creates the memory cache this manager stores into, holding at most <see cref="CacheConfig.MaxEntries" /> keys.
    ///     It is not the shared <see cref="IMemoryCache" />: with a size limit there, every Set from other code
    ///     that gives no size would throw.
    /// </summary>
    public static IMemoryCache CreateMemoryCache(CacheConfig cacheConfig)
    {
        return new MemoryCache(new MemoryCacheOptions {
            SizeLimit = cacheConfig.MaxEntries > 0 ? cacheConfig.MaxEntries : null
        });
    }

    #endregion

    #region Utilities

    private void SetEntry<T>(string key, T value, int cacheTime)
    {
        _cache.Set(key, value, GetMemoryCacheEntryOptions(cacheTime));

        //the eviction callback of an older entry under this key may have dropped the key meanwhile;
        //an entry refused at the size limit is not stored, so it is not tracked either
        if (_cache.TryGetValue(key, out _))
            CacheEntries.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
    }

    private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(int cacheTime)
    {
        //each entry counts as one key against the size limit
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTime), Size = 1 }
            .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token))
            .RegisterPostEvictionCallback(PostEvictionCallback);

        return options;
    }

    private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
    {
        if (reason == EvictionReason.Replaced) return;

        //MemoryCache runs this callback on the thread pool, often after the same key has been stored
        //again; the newer entry must stay tracked, or RemoveByPrefix would never remove it
        var cacheKey = key.ToString();
        CacheEntries.TryRemove(cacheKey, out _);
        if (_cache.TryGetValue(cacheKey, out _))
            CacheEntries.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
    }

    #endregion
}