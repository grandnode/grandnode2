using Grand.Data;
using Grand.Infrastructure;
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
        //Configure is synchronous, so the startup path has to block here
        if (!featureManager.IsEnabledAsync("Grand.Module.Migration").GetAwaiter().GetResult())
            return;

        //IMigrationProcess is scoped - resolving it from the root provider fails scope validation
        //and otherwise roots the repository graph for the lifetime of the process
        using var scope = application.Services.CreateScope();
        var migrationProcess = scope.ServiceProvider.GetRequiredService<IMigrationProcess>();
        migrationProcess.RunMigrationProcess();
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}