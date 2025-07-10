using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Configuration;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.ExternalOrderApi.Services;
using System.Text.Json.Serialization;

namespace Order.ExternalOrderApi;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IExternalOrderService, ExternalOrderService>();
        services.AddTransient<IPlugin, ExternalOrderApiPlugin>();
    }

    public int Priority => 10;

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public bool BeforeConfigure => false;

}
