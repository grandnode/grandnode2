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
                _cache.Set(key, cacheEntry, GetMemoryCacheEntryOptions(cacheTime));
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
                _cache.Set(key, cacheEntry, GetMemoryCacheEntryOptions(cacheTime));
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
            _cache.Set(key, cacheEntry, GetMemoryCacheEntryOptions(cacheTime));
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

    #endregion

    #region Utilities

    private MemoryCacheEntryOptions GetMemoryCacheEntryOptions(int cacheTime)
    {
        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTime) }
            .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token))
            .RegisterPostEvictionCallback(PostEvictionCallback);

        return options;
    }

    private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
    {
        if (reason != EvictionReason.Replaced)
            CacheEntries.TryRemove(key.ToString(), out var _);
    }

    #endregion
}