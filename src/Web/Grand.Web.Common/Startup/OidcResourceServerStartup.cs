using Grand.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.Startup;

/// <summary>
///     OIDC resource server: protected resource registry, startup validation (fails outside Development) and the
///     RFC 9728 metadata endpoints
/// </summary>
public class OidcResourceServerStartup : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IOidcProtectedResourceRegistry, OidcProtectedResourceRegistry>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        var config = application.Services.GetRequiredService<OidcResourceServerConfig>();
        if (!config.Enabled)
            return;

        var registry = application.Services.GetRequiredService<IOidcProtectedResourceRegistry>();
        var errors = OidcResourceServerConfigValidator.Validate(config, registry, webHostEnvironment.IsDevelopment());
        if (errors.Count > 0)
        {
            var message = string.Join(" ", errors);
            if (!webHostEnvironment.IsDevelopment())
                throw new InvalidOperationException(message);
            application.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(OidcResourceServerStartup)).LogWarning("{Message}", message);
        }

        OidcProtectedResourceMetadata.Map(application);
    }

    public int Priority => 501;
    public bool BeforeConfigure => false;
}
