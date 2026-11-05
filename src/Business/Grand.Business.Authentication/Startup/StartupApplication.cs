using Grand.Business.Authentication.Services;
using Grand.Business.Authentication.Services.Scim;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IScimGroupService, ScimGroupService>();
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}