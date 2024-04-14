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
    private readonly string _taskType;

    public BackgroundServiceTask(string taskType, IServiceProvider serviceProvider)
    {
        _taskType = taskType;
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
                var task = await scheduleTaskService.GetTaskByType(_taskType);
                if (task == null)
                {
                    logger.LogInformation("Task {TaskType} is not exists in the database", _taskType);
                    break;
                }

                var machineName = Environment.MachineName;
                var timeInterval = task.TimeInterval > 0 ? task.TimeInterval : 1;
                if (task.Enabled && (string.IsNullOrEmpty(task.LeasedByMachineName) ||
                                     machineName == task.LeasedByMachineName))
                {
                    var typeofTask = Type.GetType(_taskType);
                    if (typeofTask != null)
                    {
                        var scheduleTask = serviceProvider.GetServices<IScheduleTask>()
                            .FirstOrDefault(x => x.GetType() == typeofTask);
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
                                    //TODO - add settings
                                    //logger.Information($"Task {_taskType} execute");
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
                            logger.LogError("Type {TaskType} is not registered", _taskType);
                        }
                    }
                    else
                    {
                        task.Enabled = !task.StopOnError;
                        task.LastNonSuccessEndUtc = DateTime.UtcNow;
                        logger.LogError("Type {TaskType} is null (type not exists)", _taskType);
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
        var workContext = serviceProvider.GetRequiredService<IWorkContextSetter>();
        var storeHelper = serviceProvider.GetRequiredService<IStoreHelper>();
        await workContext.SetCurrentCustomer();
        await storeHelper.SetCurrentStore(scheduleTask.StoreId);
    }
}