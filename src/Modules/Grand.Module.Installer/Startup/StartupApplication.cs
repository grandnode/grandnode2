using Grand.Infrastructure;
using Grand.Module.Installer.Interfaces;
using Grand.Module.Installer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Installer.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterInstallService(services);
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;

    private void RegisterInstallService(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IInstallationLocalizedService, InstallationLocalizedService>();
        serviceCollection.AddScoped<IInstallationService, InstallationService>();
        serviceCollection.AddTransient<IDatabaseFactoryContext, DatabaseFactoryContext>();
    }
}