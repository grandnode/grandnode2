using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable CS0618 //API users stay supported until removed

namespace Grand.Business.Authentication.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IGrandAuthenticationService, CookieAuthenticationService>();
        services.AddScoped<ICookieOptionsFactory, CookieOptionsFactory>();
        services.AddScoped<IApiAuthenticationService, ApiAuthenticationService>();
        services.AddScoped<IJwtBearerAuthenticationService, JwtBearerAuthenticationService>();
        services.AddScoped<ITwoFactorAuthenticationService, TwoFactorAuthenticationService>();
        services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();
        services.AddScoped<IOidcCustomerResolver, OidcCustomerResolver>();
        services.AddScoped<IJwtBearerCustomerAuthenticationService, JwtBearerCustomerAuthenticationService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IOidcTokenRevocationService, OidcTokenRevocationService>();
        services.AddScoped<IOidcSessionService, OidcSessionService>();
        services.AddScoped<IExternalSignInPolicy, OidcSsoSignInPolicy>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        var problem = OidcSignInModeCheck.Problem(application.Services.GetService<OidcLoginConfig>(),
            application.Services.GetService<OidcResourceServerConfig>(), PluginManager.ReferencedPlugins);
        if (problem != null)
            application.Services.GetRequiredService<ILogger<OidcSsoSignInPolicy>>().LogError(
                "ProviderOnly sign-in mode without a working OpenID Connect login ({Problem}): only break-glass accounts can sign in",
                problem);
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}