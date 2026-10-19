using Grand.Infrastructure;
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
        //migrations run asynchronously in the host's starting phase, before the server starts
        services.AddHostedService<MigrationHostedService>();
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;
}