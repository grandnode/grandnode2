using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Grand.Web
{
    /// <summary>
    /// Represents startup class of application
    /// </summary>
    public class Startup
    {
        #region Properties

        /// <summary>
        /// Get configuration root of the application
        /// </summary>
        public IConfiguration Configuration { get; }

        #endregion

        #region Ctor

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            //create logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
            .CreateLogger();
        }

        #endregion

        /// <summary>
        /// Add services to the application and configure service provider
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        public void ConfigureServices(IServiceCollection services)
        {
            Grand.Infrastructure.StartupBase.ConfigureServices(services, Configuration);
        }

        /// <summary>
        /// Configure the application HTTP request pipeline
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="env">IWebHostEnvironment</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {
            Grand.Infrastructure.StartupBase.ConfigureRequestPipeline(application, webHostEnvironment);
        }
    }
}
