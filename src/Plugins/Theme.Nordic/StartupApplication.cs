using Grand.Infrastructure;
using Grand.Web.Common.Themes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Theme.Nordic;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IThemeView, NordicThemeView>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;
}