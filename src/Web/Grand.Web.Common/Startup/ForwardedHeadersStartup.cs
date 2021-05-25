using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup
{
    public class ForwardedHeadersStartup : IStartupApplication
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {

        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            //check whether database is installed
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            var serviceProvider = application.ApplicationServices;
            var hostingConfig = serviceProvider.GetRequiredService<HostingConfig>();

            if (hostingConfig.UseForwardedHeaders)
                application.UseGrandForwardedHeaders();

        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => -20;
        public bool BeforeConfigure => true;

    }
}
