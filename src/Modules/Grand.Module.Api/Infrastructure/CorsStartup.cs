using Grand.Infrastructure;
using Grand.Module.Api.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.Api.Infrastructure;

public class CorsStartup : IStartupApplication
{
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        application.UseCors(Configurations.CorsPolicyName);
    }

    public void ConfigureServices(IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(Configurations.CorsPolicyName,
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
    }

    public int Priority => 0;
    public bool BeforeConfigure => true;
}