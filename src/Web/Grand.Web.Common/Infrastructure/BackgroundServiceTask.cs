using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Tasks;
using Grand.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.Infrastructure;

public class BackgroundServiceTask : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string Name;

    public BackgroundServiceTask(string name, IServiceProvider serviceProvider)
    {
        Name = name;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var serviceProvider = scope.ServiceProvider;
                var logger = serviceProvider.GetService<ILogger<BackgroundServiceTask>>();
                var scheduleTaskService = serviceProvider.GetService<IScheduleTaskService>();
                var task = await scheduleTaskService.GetTaskByName(Name);
                if (task == null)
                {
                    logger.LogInformation("Task {TaskName} is not exists in the database", Name);
                    break;
                }

                var machineName = Environment.MachineName;
                var timeInterval = task.TimeInterval > 0 ? task.TimeInterval : 1;
                if (task.Enabled && (string.IsNullOrEmpty(task.LeasedByMachineName) ||
                                     machineName == task.LeasedByMachineName))
                {

                    var scheduleTask = serviceProvider.GetRequiredKeyedService<IScheduleTask>(task.ScheduleTaskName);
                    if (scheduleTask != null)
                    {
                        //assign current customer (background task) / current store (from task)
                        await WorkContext(serviceProvider, task);
                        var runTask = true;
                        if (task.LastStartUtc.HasValue)
                        {
                            var dateTimeNow = DateTime.UtcNow;
                            if (dateTimeNow < task.LastStartUtc.Value.AddMinutes(task.TimeInterval))
                            {
                                runTask = false;
                                timeInterval =
                                    (int)(task.LastStartUtc.Value.AddMinutes(task.TimeInterval) - dateTimeNow)
                                    .TotalMinutes;
                            }
                            else
                            {
                                runTask = true;
                                timeInterval = task.TimeInterval > 0 ? task.TimeInterval : 1;
                            }
                        }

                        if (runTask)
                        {
                            task.LastStartUtc = DateTime.UtcNow;
                            try
                            {                                
                                logger.LogInformation($"Task {Name} execute");
                                await scheduleTask.Execute();
                                task.LastSuccessUtc = DateTime.UtcNow;
                                task.LastNonSuccessEndUtc = null;
                            }
                            catch (Exception exc)
                            {
                                task.LastNonSuccessEndUtc = DateTime.UtcNow;
                                task.Enabled = !task.StopOnError;
                                logger.LogError(exc,
                                    "Error while running the \'{TaskScheduleTaskName}\' schedule task",
                                    task.ScheduleTaskName);
                            }
                        }
                    }
                    else
                    {
                        task.Enabled = !task.StopOnError;
                        task.LastNonSuccessEndUtc = DateTime.UtcNow;
                        logger.LogError("Type {TaskName} is not registered", Name);
                    }

                    await scheduleTaskService.UpdateTask(task);
                    await Task.Delay(TimeSpan.FromMinutes(timeInterval), stoppingToken);
                }
                else
                {
                    break;
                }
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
    }

    private async Task WorkContext(IServiceProvider serviceProvider, ScheduleTask scheduleTask)
    {
        var contextAccessor = serviceProvider.GetRequiredService<IContextAccessor>();
        
        var storeContext = serviceProvider.GetRequiredService<IStoreContextSetter>();
        contextAccessor.StoreContext = await storeContext.InitializeStoreContext(scheduleTask.StoreId);

        var workContext = serviceProvider.GetRequiredService<IWorkContextSetter>();
        contextAccessor.WorkContext = await workContext.InitializeWorkContext(scheduleTask.StoreId);
    }
}