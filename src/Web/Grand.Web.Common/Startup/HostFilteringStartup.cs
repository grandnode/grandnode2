using Grand.Domain.Data;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring host filtering middleware on application 
    /// </summary>
    public class HostFilteringStartup : IStartupApplication
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var hosts = configuration["Hosting:AllowedHosts"]?
                        .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (hosts?.Length > 0)
            {
                services.Configure<HostFilteringOptions>(options => options.AllowedHosts = hosts);
            }
        }

        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            //check whether database is installed
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //configure authentication
            application.UseHostFiltering();
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => -40;
        public bool BeforeConfigure => true;

    }
}
