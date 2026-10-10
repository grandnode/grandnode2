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
        var config = new OidcLoginConfig();
        configuration.GetSection(OidcLoginDefaults.ConfigSection).Bind(config);
        services.AddSingleton(config);

        services.AddScoped<IExternalAuthenticationProvider, OidcLoginProvider>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;
}
