using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace Grand.Infrastructure.Caching
{
    public static class CacheEntryExtensions
    {
        public static ICacheEntry SetKey(this ICacheEntry entry, ConcurrentDictionary<string, ICacheEntry> _cacheEntries)
        {
            _cacheEntries.AddOrUpdate(entry.Key.ToString(), entry, (o, cacheEntry) =>
            {
                cacheEntry.Value = entry;
                return cacheEntry;
            });
            return entry;
        }
    }
}
