using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Startup;

/// <summary>
///     Browser sign-in with the identity provider of the resource server; the sign-in method exists only when
///     OidcResourceServer:Authority and OidcLogin:ClientId are set
/// </summary>
public class OidcLoginStartup : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        //no redirects: a 307/308 from the token endpoint would re-post the refresh token and client secret elsewhere
        services.AddHttpClient(OidcLoginDefaults.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        //the sign-in controller and the back-channel logout endpoint are always mapped
        services.AddSingleton<OidcLogoutTokenValidator>();
        services.AddScoped<OidcProfileSynchronizer>();

        var resourceServer = new OidcResourceServerConfig();
        configuration.GetSection("OidcResourceServer").Bind(resourceServer);
        var login = new OidcLoginConfig();
        configuration.GetSection(OidcLoginDefaults.ConfigSection).Bind(login);
        if (!login.IsConfigured(resourceServer))
            return;

        services.AddScoped<IExternalAuthenticationProvider, OidcLoginProvider>();
        services.AddScoped<IOidcTokenRefresher, OidcTokenRefresher>();
        services.AddScoped<IExternalSignOutProvider, OidcSignOutProvider>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 502;
    public bool BeforeConfigure => false;
}
