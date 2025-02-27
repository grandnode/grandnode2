using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Infrastructure;
using Grand.Web.Common.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Grand.Web.Common.Startup;

/// <summary>
///     Represents object for the configuring common features and middleware on application startup
/// </summary>
public class GrandCommonStartup : IStartupApplication
{
    /// <summary>
    ///     Add and configure any of the middleware
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="configuration">Configuration root of the application</param>
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var securityConfig = new SecurityConfig();
        configuration.GetSection("Security").Bind(securityConfig);

        //add settings
        services.AddSettings();

        //compression
        services.AddResponseCompression();

        //add options feature
        services.AddOptions();

        //add HTTP session state feature
        services.AddHttpSession(securityConfig);

        //add anti-forgery
        services.AddAntiForgery(securityConfig);

        //add localization
        services.AddLocalization();

        //add theme support
        services.AddThemes();

        //add WebEncoderOptions
        services.AddWebEncoder();

        //add detection device
        services.AddDetectionDevice();

        //add routing
        services.AddRouting(options =>
        {
            options.ConstraintMap["lang"] = typeof(LanguageParameterTransformer);
        });

        //add data protection
        services.AddGrandDataProtection(configuration);
    }

    /// <summary>
    ///     Configure the using of added middleware
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    /// <param name="webHostEnvironment">WebHostEnvironment</param>
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        var appConfig = application.Services.GetRequiredService<AppConfig>();
        var performanceConfig = application.Services.GetRequiredService<PerformanceConfig>();
        var securityConfig = application.Services.GetRequiredService<SecurityConfig>();
        var featureManager = application.Services.GetRequiredService<IFeatureManager>();

        //add HealthChecks
        application.UseGrandHealthChecks();

        //default security headers
        if (securityConfig.UseDefaultSecurityHeaders) application.UseDefaultSecurityHeaders();

        //use hsts
        if (securityConfig.UseHsts) application.UseHsts();
        //enforce HTTPS in ASP.NET Core
        if (securityConfig.UseHttpsRedirection) application.UseHttpsRedirection();

        //compression
        if (performanceConfig.UseResponseCompression)
            //gzip by default
            application.UseResponseCompression();

        //use static files feature
        application.UseGrandStaticFiles(appConfig);

        //install middleware
        application.UseInstallUrl();

        //use HTTP session
        application.UseSession();

        //use powered by
        if (!performanceConfig.IgnoreUsePoweredByMiddleware)
            application.UsePoweredBy();

        //add responsive middleware (for detection)
        application.UseDetection();

        //use routing
        application.UseRouting();
    }

    /// <summary>
    ///     Gets order of this startup configuration implementation
    /// </summary>
    public int Priority => 100;

    public bool BeforeConfigure => true;
}