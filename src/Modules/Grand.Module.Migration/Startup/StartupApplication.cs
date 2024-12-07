using Grand.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Migrations;
using Grand.Module.Migration.Migrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace Grand.Module.Migration.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMigrationProcess, MigrationProcess>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;
        var featureManager = application.Services.GetRequiredService<IFeatureManager>();
        if (featureManager.IsEnabledAsync("Grand.Module.Migration").Result)
        {
            var migrationProcess = application.Services.GetRequiredService<IMigrationProcess>();
            migrationProcess.RunMigrationProcess();
        }
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}