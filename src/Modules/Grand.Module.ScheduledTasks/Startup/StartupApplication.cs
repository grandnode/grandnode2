using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Module.ScheduledTasks.BackgroundServices;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Module.ScheduledTasks.Startup;

public class StartupApplication : IStartupApplication
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        RegisterTask(services);
    }

    public void Configure(WebApplication application, IWebHostEnvironment webHostEnvironment)
    {
    }

    public int Priority => 100;
    public bool BeforeConfigure => false;

    private void RegisterTask(IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<IScheduleTaskService, ScheduleTaskService>();

        serviceCollection.AddKeyedScoped<IScheduleTask, QueuedMessagesSendScheduleTask>("Send emails");
        serviceCollection.AddKeyedScoped<IScheduleTask, ClearCacheScheduleTask>("Clear cache");
        serviceCollection.AddKeyedScoped<IScheduleTask, GenerateSitemapXmlTask>("Generate sitemap XML file");
        serviceCollection.AddKeyedScoped<IScheduleTask, DeleteGuestsScheduleTask>("Delete guests");
        serviceCollection.AddKeyedScoped<IScheduleTask, UpdateExchangeRateScheduleTask>("Update currency exchange rates");
        serviceCollection.AddKeyedScoped<IScheduleTask, EndAuctionsTask>("End of the auctions");
        serviceCollection.AddKeyedScoped<IScheduleTask, CancelOrderScheduledTask>("Cancel unpaid and pending orders");
    }
}