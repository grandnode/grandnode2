using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Web.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup
{
    public class DbCheckStartup : IStartupApplication
    {
        public int Priority => -1000;
        public bool BeforeConfigure => true;

        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            application.UseMiddleware<DbVersionCheckMiddleware>();
        }

        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
        }
    }
}
