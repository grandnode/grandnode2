using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring/load url rewrite rules from external file on application startup
    /// </summary>
    public class UrlRewriteStartup : IStartupApplication
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
            var serviceProvider = application.ApplicationServices;
            var appConfig = serviceProvider.GetRequiredService<AppConfig>();
            var urlRewriteOptions = new RewriteOptions();
            var rewriteOptions = false;
            if (appConfig.UseUrlRewrite)
            {
                if (File.Exists("App_Data/UrlRewrite.xml"))
                {
                    using (var streamReader = File.OpenText("App_Data/UrlRewrite.xml"))
                    {
                        rewriteOptions = true;
                        urlRewriteOptions.AddIISUrlRewrite(streamReader);
                    }
                }
            }
            if (appConfig.UrlRewriteHttpsOptions)
            {
                rewriteOptions = true;
                urlRewriteOptions.AddRedirectToHttps(appConfig.UrlRewriteHttpsOptionsStatusCode, appConfig.UrlRewriteHttpsOptionsPort);
            }
            if (appConfig.UrlRedirectToHttpsPermanent)
            {
                rewriteOptions = true;
                urlRewriteOptions.AddRedirectToHttpsPermanent();
            }
            if (rewriteOptions)
                application.UseRewriter(urlRewriteOptions);
        }

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        public int Priority => -50;
        public bool BeforeConfigure => true;

    }
}
