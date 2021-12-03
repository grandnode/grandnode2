using Grand.Business.System.Interfaces.ScheduleTasks;
using Grand.Business.System.Services.BackgroundServices;
using Grand.Domain.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Grand.Web.Common.Startup
{
    public static class TaskHandlerHelper
    {

        public static void RegisterTasks(this IServiceCollection services)
        {
            //database is already installed, so start scheduled tasks
            if (DataSettingsManager.DatabaseIsInstalled())
            {
                var appConfig = services.BuildServiceProvider().GetRequiredService<AppConfig>();
                if (!appConfig.DisableHostedService)
                {
                    var typeSearcher = new AppTypeSearcher();
                    var scheduleTasks = typeSearcher.ClassesOfType<IScheduleTask>();

                    var scheduleTasksInstalled = scheduleTasks
                    .Where(t => PluginExtensions.OnlyInstalledPlugins(t)); //ignore not installed plugins

                    foreach (var task in scheduleTasksInstalled)
                    {
                        var assemblyName = task.Assembly.GetName().Name;
                        services.AddSingleton<IHostedService, BackgroundServiceTask>(sp =>
                        {
                            return new BackgroundServiceTask($"{task.FullName}, {assemblyName}", sp);
                        });
                    }
                }
            }
        }
    }
}
