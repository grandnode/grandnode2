using Grand.Infrastructure;
using Grand.Module.Api.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grand.Module.Api.Infrastructure;

public class CorsStartup : IStartupApplication
{
    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
        if(webHostEnvironment.IsDevelopment())
            application.UseCors(Configurations.DevelopmentCorsPolicyName);
        else
            application.UseCors(Configurations.ProductionCorsPolicyName);
    }

    public void ConfigureServices(IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("AllowedHostOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(Configurations.DevelopmentCorsPolicyName,
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            options.AddPolicy(Configurations.ProductionCorsPolicyName,
                builder => builder.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader());
        });
    }

    public int Priority => 0;
    public bool BeforeConfigure => true;
}