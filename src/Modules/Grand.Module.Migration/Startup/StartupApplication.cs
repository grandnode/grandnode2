using Grand.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Migrations;
using Grand.Module.Migration.Migrations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Migration.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IMigrationProcess, MigrationProcess>();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
        if (!DataSettingsManager.DatabaseIsInstalled())
            return;

        var serviceProvider = application.ApplicationServices;
        var featureFlagsConfig = serviceProvider.GetRequiredService<FeatureFlagsConfig>();
        if (featureFlagsConfig.Modules.TryGetValue("Grand.Module.Migration", out var value) && value)
        {
            var migrationProcess = serviceProvider.GetRequiredService<IMigrationProcess>();
            migrationProcess.RunMigrationProcess();
        }
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}