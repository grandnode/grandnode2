using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace Grand.Infrastructure.Caching;

/// <summary>
///     Represents a manager for memory caching
/// </summary>
public class MemoryCacheBase : ICacheBase
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

    private bool _disposed;
    private static CancellationTokenSource _resetCacheToken = new();

    protected readonly ConcurrentDictionary<string, SemaphoreSlim> CacheEntries = new();

    #endregion

    #region Methods

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

    public virtual Task RemoveAsync(string key, bool publisher = true)
    {
        _cache.Remove(key);

        if (publisher)
            _mediator.Publish(new EntityCacheEvent(key, CacheEvent.RemoveKey));

        return Task.CompletedTask;
    }

    public virtual Task RemoveByPrefix(string prefix, bool publisher = true)
    {
        var entriesToRemove = CacheEntries.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        foreach (var cacheEntries in entriesToRemove) _cache.Remove(cacheEntries.Key);

        if (publisher)
            _mediator.Publish(new EntityCacheEvent(prefix, CacheEvent.RemovePrefix));

        return Task.CompletedTask;
    }

    public virtual Task Clear(bool publisher = true)
    {
        //clear keys
        foreach (var cacheEntry in CacheEntries.Keys.ToList())
            _cache.Remove(cacheEntry);

        //cancel
        _resetCacheToken.Cancel();
        //dispose
        _resetCacheToken.Dispose();

        _resetCacheToken = new CancellationTokenSource();

        return Task.CompletedTask;
    }

    ~MemoryCacheBase()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing) _cache.Dispose();
        _disposed = true;
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