using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Authentication.OpenIdConnect.Infrastructure;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IExternalAuthenticationProvider, OidcLoginProvider>();
        //no redirects: a 307/308 from the token endpoint would re-post the refresh token and client secret elsewhere
        services.AddHttpClient(OidcLoginDefaults.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<IOidcTokenRefresher, OidcTokenRefresher>();
        services.AddSingleton<OidcLogoutTokenValidator>();
        services.AddScoped<OidcProfileSynchronizer>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;
}
