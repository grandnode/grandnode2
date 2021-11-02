using Grand.Business.System.Interfaces.ScheduleTasks;
using Grand.Business.System.Services.BackgroundServices;
using Grand.Domain.Data;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring task on application startup
    /// </summary>
    public static class TaskHandlerHelper
    {
        /// <summary>
        /// Add and configure any of the register tasks
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public static void RegisterTasks(this IServiceCollection services)
        {
            //database is already installed, so start scheduled tasks
            if (DataSettingsManager.DatabaseIsInstalled())
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
