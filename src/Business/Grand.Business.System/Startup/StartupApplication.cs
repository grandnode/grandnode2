using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Business.System.Services.BackgroundServices.ScheduleTasks;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterTask(services);
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;

    private void RegisterTask(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScheduleTaskService, ScheduleTaskService>();

        serviceCollection.AddScoped<IScheduleTask, QueuedMessagesSendScheduleTask>();
        serviceCollection.AddScoped<IScheduleTask, ClearCacheScheduleTask>();
        serviceCollection.AddScoped<IScheduleTask, GenerateSitemapXmlTask>();
        serviceCollection.AddScoped<IScheduleTask, DeleteGuestsScheduleTask>();
        serviceCollection.AddScoped<IScheduleTask, UpdateExchangeRateScheduleTask>();
        serviceCollection.AddScoped<IScheduleTask, EndAuctionsTask>();
        serviceCollection.AddScoped<IScheduleTask, CancelOrderScheduledTask>();
    }
}