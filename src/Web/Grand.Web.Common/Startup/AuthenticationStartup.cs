using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Infrastructure;
using Grand.Web.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring authentication middleware on application startup
    /// </summary>
    public class AuthenticationStartup : IStartupApplication
    {
        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            var config = new AppConfig();
            configuration.GetSection("Application").Bind(config);

            //add authentication
            services.AddGrandAuthentication(configuration);
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
            application.UseGrandAuthentication();

            //set workcontext
            application.UseMiddleware<WorkContextMiddleware>();

        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => 500;
        public bool BeforeConfigure => true;

    }
}
