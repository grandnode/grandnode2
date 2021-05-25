using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Linq;

namespace Grand.Web.Common.Middleware
{
    public class DbVersionCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public DbVersionCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(
            HttpContext context, 
            ICacheBase cacheBase, 

            IRepository<GrandNodeVersion> repository)
        {
            if (context == null || context.Request == null)
            {
                await _next(context);
                return;
            }

            var version = cacheBase.Get(CacheKey.GRAND_NODE_VERSION, () => repository.Table.FirstOrDefault());
            if (version == null)
            {
                await context.Response.WriteAsync($"The database does not exist.");
                return;
            }

            if (!version.DataBaseVersion.Equals(GrandVersion.SupportedDBVersion))
            {
                await context.Response.WriteAsync($"The database version is not supported in this software version. " +
                    $"Supported version: {GrandVersion.SupportedDBVersion} , your version: {version.DataBaseVersion}");
            }
            else
            {
                await _next(context);
            }
        }
    }
}
