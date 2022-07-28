using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;
using Grand.Web.Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Grand.Web.Common.Startup
{
    public static class TaskHandler
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
