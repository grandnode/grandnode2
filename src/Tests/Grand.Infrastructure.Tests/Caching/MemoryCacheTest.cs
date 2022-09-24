using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Infrastructure.Tests.Caching
{
    public static class MemoryCacheTest
    {
        private static IMemoryCache _memoryCache;
        public static IMemoryCache Get()
        {
            CommonHelper.CacheTimeMinutes = 1;
            if (_memoryCache == null)
            {
                var services = new ServiceCollection();
                services.AddMemoryCache();
                var serviceProvider = services.BuildServiceProvider();

                _memoryCache = serviceProvider.GetService<IMemoryCache>();
            }
            return _memoryCache;
        }
    }
}
