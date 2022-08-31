using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;

namespace Grand.Infrastructure.Caching
{
    /// <summary>
    /// Represents a manager for memory caching
    /// </summary>
    public partial class MemoryCacheBase : ICacheBase
    {
        #region Fields

        private readonly IMemoryCache _cache;
        private readonly IMediator _mediator;

        private bool _disposed;
        private static CancellationTokenSource _resetCacheToken = new();

        protected readonly ConcurrentDictionary<string, SemaphoreSlim> _cacheEntries = new();

        #endregion

        #region Ctor

        public MemoryCacheBase(IMemoryCache cache, IMediator mediator)
        {
            _cache = cache;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        public virtual Task<T> GetAsync<T>(string key, Func<Task<T>> acquire)
        {
            return GetAsync(key, acquire, CommonHelper.CacheTimeMinutes);
        }

        public virtual async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime)
        {
            T cacheEntry;
            if (!_cache.TryGetValue(key, out cacheEntry))
            {
                SemaphoreSlim slock = _cacheEntries.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
                await slock.WaitAsync();
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
                    slock.Release();
                }
            }
            return cacheEntry;
        }

        public virtual T Get<T>(string key, Func<T> acquire)
        {
            return Get<T>(key, acquire, CommonHelper.CacheTimeMinutes);
        }

        public virtual T Get<T>(string key, Func<T> acquire, int cacheTime)
        {
            T cacheEntry;
            if (!_cache.TryGetValue(key, out cacheEntry))
            {
                SemaphoreSlim slock = _cacheEntries.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
                slock.Wait();
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
                    slock.Release();
                }
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
            var entriesToRemove = _cacheEntries.Where(x => x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            foreach (var cacheEntrie in entriesToRemove)
            {
                _cache.Remove(cacheEntrie.Key);
            }

            if (publisher)
                _mediator.Publish(new EntityCacheEvent(prefix, CacheEvent.RemovePrefix));

            return Task.CompletedTask;
        }

        public virtual Task Clear(bool publisher = true)
        {
            //clear keys
            foreach (var cacheEntry in _cacheEntries.Keys.ToList())
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
            if (!_disposed)
            {
                if (disposing)
                {
                    _cache.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        #region Utilities

        protected MemoryCacheEntryOptions GetMemoryCacheEntryOptions(int cacheTime)
        {
            var options = new MemoryCacheEntryOptions() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTime) }
                .AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token))
                .RegisterPostEvictionCallback(PostEvictionCallback);

            return options;
        }

        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (reason != EvictionReason.Replaced)
                _cacheEntries.TryRemove(key.ToString(), out var _);
            
        }

        #endregion

    }

}