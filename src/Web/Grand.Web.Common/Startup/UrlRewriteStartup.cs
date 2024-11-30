using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup;

/// <summary>
///     Represents object for the configuring/load url rewrite rules from external file on application startup
/// </summary>
public class UrlRewriteStartup : IStartupApplication
{
    private const string UrlRewriteFilePath = "App_Data/UrlRewrite.xml";

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
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        var urlConfig = application.Services.GetRequiredService<UrlRewriteConfig>();
        var urlRewriteOptions = new RewriteOptions();
        
        ConfigureUrlRewriteOptions(urlRewriteOptions, urlConfig);
        ConfigureHttpsOptions(urlRewriteOptions, urlConfig);

        if (urlRewriteOptions.Rules.Count > 0)
        {
            application.UseRewriter(urlRewriteOptions);
        }
    }
    private void ConfigureUrlRewriteOptions(RewriteOptions options, UrlRewriteConfig config)
    {
        if (config.UseUrlRewrite && File.Exists(UrlRewriteFilePath))
        {
            using var streamReader = File.OpenText(UrlRewriteFilePath);
            options.AddIISUrlRewrite(streamReader);
        }
    }

    private void ConfigureHttpsOptions(RewriteOptions options, UrlRewriteConfig config)
    {
        if (config.UrlRewriteHttpsOptions)
        {
            options.AddRedirectToHttps(config.UrlRewriteHttpsOptionsStatusCode, config.UrlRewriteHttpsOptionsPort);
        }

        if (config.UrlRedirectToHttpsPermanent)
        {
            options.AddRedirectToHttpsPermanent();
        }
    }
    /// <summary>
    ///     Gets order of this startup configuration implementation
    /// </summary>
    public int Priority => -50;

    public bool BeforeConfigure => true;
}