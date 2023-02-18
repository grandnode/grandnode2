using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Infrastructure.Tests.Caching
{
    public static class MemoryCacheTest
    {
        public static IMemoryCache Get()
        {
            CommonHelper.CacheTimeMinutes = 1;
            var services = new ServiceCollection();
            services.AddMemoryCache();
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider.GetService<IMemoryCache>();
        }
    }
}
