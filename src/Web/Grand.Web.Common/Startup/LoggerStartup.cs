using Grand.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup;

/// <summary>
///     Represents object for the configuring logger middleware on application startup
/// </summary>
public class LoggerStartup : IStartupApplication
{
    /// <summary>
    ///     Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration root of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    /// <summary>
    ///     Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    /// <param name="webHostEnvironment">WebHostEnvironment</param>
    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
        //check whether database is installed
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        var appConfig = application.ApplicationServices.GetRequiredService<AppConfig>();

        //set context logging
        if (appConfig.EnableContextLoggingMiddleware)
            application.UseMiddleware<ContextLoggingMiddleware>();
    }

    /// <summary>
    ///     Gets order of this startup configuration implementation
    /// </summary>
    public int Priority => 501;

    public bool BeforeConfigure => false;
}