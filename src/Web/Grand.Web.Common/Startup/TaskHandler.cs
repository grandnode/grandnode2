﻿using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Infrastructure.TypeSearch;
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
            if (!DataSettingsManager.DatabaseIsInstalled()) return;
            var appConfig = services.BuildServiceProvider().GetRequiredService<AppConfig>();
            if (appConfig.DisableHostedService) return;
            var typeSearcher = new TypeSearcher();
            var scheduleTasks = typeSearcher.ClassesOfType<IScheduleTask>();

            var scheduleTasksInstalled = scheduleTasks
                .Where(PluginExtensions.OnlyInstalledPlugins); //ignore not installed plugins

            foreach (var task in scheduleTasksInstalled)
            {
                var assemblyName = task.Assembly.GetName().Name;
                services.AddSingleton<IHostedService, BackgroundServiceTask>(sp => new BackgroundServiceTask($"{task.FullName}, {assemblyName}", sp));
            }
        }
    }
}
