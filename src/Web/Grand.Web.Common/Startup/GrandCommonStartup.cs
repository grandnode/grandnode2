using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Infrastructure;
using Grand.Web.Common.RequestCustomerResolvers;
using Grand.Web.Common.Routing;
using Grand.Web.Common.UserAgent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using System.Threading.RateLimiting;

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

        //classify the request's user agent (crawler, mobile)
        services.AddScoped<IUserAgentClassifier, UserAgentClassifier>();

        //a guest is saved only once it returns its cookie or an action marked [PersistGuestCustomer] writes to it
        services.AddScoped<IGuestCustomerPersistence, GuestCustomerPersistence>();

        //add routing
        services.AddRouting(options =>
        {
            options.ConstraintMap["lang"] = typeof(LanguageParameterTransformer);
        });

        //add data protection
        services.AddGrandDataProtection(configuration);

        //limits only the actions that name a policy; per instance, kept in memory
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            //with a body: the store shows no page of its own for this status
            options.OnRejected = (context, cancellationToken) =>
                new ValueTask(context.HttpContext.Response.WriteAsync(
                    "Too many requests. Please try again later.", cancellationToken));
            //a guest asking for a return link: guessing order numbers and e-mail addresses takes many tries
            options.AddPolicy(GuestMerchandiseReturnRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "",
                    _ => new FixedWindowRateLimiterOptions
                        { PermitLimit = 5, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));
        });
    }

    /// <summary>
    ///     The rate limiter policy of the page where guests ask for a merchandise return link
    /// </summary>
    public const string GuestMerchandiseReturnRateLimitPolicy = "GuestMerchandiseReturn";

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

        //use routing
        application.UseRouting();

        //after routing: a policy is named by the endpoint
        application.UseRateLimiter();
    }

    /// <summary>
    ///     Gets order of this startup configuration implementation
    /// </summary>
    public int Priority => 100;

    public bool BeforeConfigure => true;
}