using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Grand.Infrastructure.Caching
{
    public static class CacheEntryExtensions
    {
        public static ICacheEntry SetKey(this ICacheEntry entry, ConcurrentDictionary<object, ICacheEntry> _cacheEntries)
        {
            _cacheEntries.AddOrUpdate(entry.Key, entry, (o, cacheEntry) =>
            {
                cacheEntry.Value = entry;
                return cacheEntry;
            });
            return entry;
        }
    }
}
