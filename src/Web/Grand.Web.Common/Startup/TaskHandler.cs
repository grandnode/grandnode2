using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Data;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grand.Web.Common.Startup;

public static class TaskHandler
{
    public static void RegisterTasks(this IServiceCollection services, IConfiguration configuration)
    {
        //database is already installed, so start scheduled tasks
        if (!DataSettingsManager.DatabaseIsInstalled()) return;

        var scheduleTaskKeyedServices = KeyedServiceHelper.GetKeyedServicesForInterface<IScheduleTask>(services);

        foreach (var task in scheduleTaskKeyedServices)
        {
            services.AddSingleton<IHostedService, BackgroundServiceTask>(sp => new BackgroundServiceTask(task, sp));
        }
    }
}