using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Scim.Infrastructure;

/// <summary>The SCIM 2.0 endpoint (/scim/v2) of module Grand.Module.Scim</summary>
public class ScimStartup : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ScimAuthorizeFilter>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        var config = application.Services.GetRequiredService<ScimConfig>();
        var loginConfig = application.Services.GetRequiredService<OidcLoginConfig>();
        var resourceServerConfig = application.Services.GetRequiredService<OidcResourceServerConfig>();
        if (config.TokenUsable && loginConfig.IsConfigured(resourceServerConfig))
            return;

        application.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ScimStartup))
            .LogError("Module Grand.Module.Scim is on, but Scim:Token is shorter than 32 characters or the OpenID Connect " +
                      "sign-in (OidcLogin) is not configured: every SCIM request is refused. See docs/scim.md");
    }

    public int Priority => 0;
    public bool BeforeConfigure => false;
}
