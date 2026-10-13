using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}