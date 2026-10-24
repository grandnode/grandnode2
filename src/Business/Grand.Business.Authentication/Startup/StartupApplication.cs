using Grand.Business.Authentication.Services;
using Grand.Business.Authentication.Services.Scim;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
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
        services.AddScoped<IJwtBearerAuthenticationService, JwtBearerAuthenticationService>();
        services.AddScoped<ITwoFactorAuthenticationService, TwoFactorAuthenticationService>();
        services.AddScoped<IExternalAuthenticationService, ExternalAuthenticationService>();
        services.AddScoped<IOidcCustomerResolver, OidcCustomerResolver>();
        services.AddScoped<IJwtBearerCustomerAuthenticationService, JwtBearerCustomerAuthenticationService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IOidcTokenRevocationService, OidcTokenRevocationService>();
        services.AddScoped<IOidcSessionService, OidcSessionService>();
        services.AddScoped<IOidcLinkService, OidcLinkService>();
        services.AddScoped<IExternalSignInPolicy, OidcSsoSignInPolicy>();
        services.AddScoped<ScimRoleSync>();
        services.AddScoped<ScimAccountLock>();
        services.AddScoped<IScimUserService, ScimUserService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        var problem = OidcSignInModeCheck.Problem(application.Services.GetService<OidcLoginConfig>(),
            application.Services.GetService<OidcResourceServerConfig>());
        if (problem != null)
            application.Services.GetRequiredService<ILogger<OidcSsoSignInPolicy>>().LogError(
                "ProviderOnly sign-in mode without a working OpenID Connect login ({Problem}): only break-glass accounts can sign in",
                problem);
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}