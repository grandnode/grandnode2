using Grand.Infrastructure.Events;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

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
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();

        #endregion

        #region Ctor

        public MemoryCacheBase(IMemoryCache cache, IMediator mediator)
        {
            _cache = cache;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        public virtual async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire)
        {
            return await GetAsync(key, acquire, CommonHelper.CacheTimeMinutes);
        }

        public virtual async Task<T> GetAsync<T>(string key, Func<Task<T>> acquire, int cacheTime)
        {
            return await _cache.GetOrCreateAsync(key, entry =>
            {
                entry.SetOptions(GetMemoryCacheEntryOptions(cacheTime));
                return acquire();
            });
        }

        public virtual T Get<T>(string key, Func<T> acquire)
        {
            return Get<T>(key, acquire, CommonHelper.CacheTimeMinutes);
        }

        public virtual T Get<T>(string key, Func<T> acquire, int cacheTime)
        {
            return _cache.GetOrCreate(key, entry =>
            {
                entry.SetOptions(GetMemoryCacheEntryOptions(cacheTime));
                return acquire();
            });
        }
        public virtual Task SetAsync(string key, object data, int cacheTime)
        {
            if (data != null)
            {
                _cache.Set(key, data, GetMemoryCacheEntryOptions(cacheTime));
            }
            return Task.CompletedTask;
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
            var keysToRemove = _cache.GetKeys<string>().Where(x => x.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
            }

            if (publisher)
                _mediator.Publish(new EntityCacheEvent(prefix, CacheEvent.RemovePrefix));

            return Task.CompletedTask;
        }

        public virtual Task Clear(bool publisher = true)
        {
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
            if (reason == EvictionReason.Replaced)
                return;

            if (reason == EvictionReason.TokenExpired)
                return;
        }

        #endregion

    }

}