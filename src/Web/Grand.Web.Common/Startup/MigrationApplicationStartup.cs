using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Migrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring/load migration process on application startup
    /// </summary>
    public class MigrationApplicationStartup : IStartupApplication
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
        /// Configure the using 
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            var serviceProvider = application.ApplicationServices;
            var appConfig = serviceProvider.GetRequiredService<AppConfig>();
            if (!appConfig.SkipMigrationProcess)
            {
                var migrationProcess = serviceProvider.GetRequiredService<IMigrationProcess>();
                migrationProcess.RunMigrationProcess();
            }
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => 1000;

        public bool BeforeConfigure => false;

    }
}
