using Grand.Business.System.Interfaces.ScheduleTasks;
using Grand.Business.System.Services.BackgroundServices;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearchers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

namespace Grand.Web.Common.Startup
{
    /// <summary>
    /// Represents object for the configuring task on application startup
    /// </summary>
    public class TaskHandlerStartup : IStartupApplication
    {
        /// <summary>
        /// Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        /// <param name="webHostEnvironment">WebHostEnvironment</param>
        public void Configure(IApplicationBuilder application, IWebHostEnvironment webHostEnvironment)
        {

        }

        /// <summary>
        /// Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration root of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
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

        /// <summary>
        /// Gets order of this startup configuration implementation
        /// </summary>
        /// task handlers should be loaded last
        public int Priority => 1010;
        public bool BeforeConfigure => true;

    }
}
