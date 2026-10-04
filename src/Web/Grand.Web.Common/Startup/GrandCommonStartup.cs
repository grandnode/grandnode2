using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Infrastructure;
using Grand.Web.Common.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
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
        services.AddResponseCompression(options =>
        {
            //Without this nothing is ever compressed on a site served over HTTPS, which is
            //every real store - the default is false because compressing a response that
            //mixes a secret (the antiforgery token) with attacker-influenced content is the
            //BREACH attack. The exposure is the same one taken by every CDN and reverse
            //proxy that gzips HTML; the alternative here was shipping the 399 kB script
            //bundle and 392 kB stylesheet uncompressed on every cold visit.
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            //ResponseCompressionDefaults covers application/javascript, but static files are
            //served as text/javascript, so the bundle would fall through the default list.
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat([
                "text/javascript",
                "image/svg+xml",
                "application/manifest+json"
            ]);
        });

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