using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.System.Interfaces.ScheduleTasks;
using Grand.Infrastructure;
using Grand.Domain.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.BackgroundServices
{
    public class BackgroundServiceTask : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private string _taskType;
        public BackgroundServiceTask(string tasktype, IServiceProvider serviceProvider)
        {
            _taskType = tasktype;
            _serviceProvider = serviceProvider;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var serviceProvider = scope.ServiceProvider;
                    var logger = serviceProvider.GetService<ILogger>();
                    var scheduleTaskService = serviceProvider.GetService<IScheduleTaskService>();
                    var task = await scheduleTaskService.GetTaskByType(_taskType);
                    if (task == null)
                    {
                        logger.Information($"Task {_taskType} is not exists in the database");
                        break;
                    }

                    var machineName = Environment.MachineName;
                    var timeInterval = task.TimeInterval > 0 ? task.TimeInterval : 1;
                    if (task.Enabled && (string.IsNullOrEmpty(task.LeasedByMachineName) || (machineName == task.LeasedByMachineName)))
                    {
                        var typeofTask = Type.GetType(_taskType);
                        if (typeofTask != null)
                        {
                            var scheduleTask = serviceProvider.GetServices<IScheduleTask>().FirstOrDefault(x => x.GetType() == typeofTask);
                            if (scheduleTask != null)
                            {

                                //assign current customer (background task) / current store (from task)
                                await WorkContext(serviceProvider, task);
                                bool runTask = true;
                                if(task.LastStartUtc.HasValue)
                                {
                                    if (DateTime.UtcNow < task.LastStartUtc.Value.AddMinutes(task.TimeInterval))
                                    {
                                        runTask = false;
                                        timeInterval = (int)(DateTime.UtcNow - task.LastStartUtc.Value).TotalMinutes;
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
                                        await logger.InsertLog(Domain.Logging.LogLevel.Error, $"Error while running the '{task.ScheduleTaskName}' schedule task", exc.Message);
                                    }
                                }
                            }
                            else
                            {
                                task.Enabled = !task.StopOnError;
                                task.LastNonSuccessEndUtc = DateTime.UtcNow;
                                await logger.InsertLog(Domain.Logging.LogLevel.Error, $"Type {_taskType} is not registered");
                            }
                        }
                        else
                        {
                            task.Enabled = !task.StopOnError;
                            task.LastNonSuccessEndUtc = DateTime.UtcNow;
                            await logger.InsertLog(Domain.Logging.LogLevel.Error, $"Type {_taskType} is null (type not exists)");
                        }
                        await scheduleTaskService.UpdateTask(task);
                        await Task.Delay(TimeSpan.FromMinutes(timeInterval), stoppingToken);
                    }
                    else
                        break;

                }
                catch (Exception ex)
                {
                    Serilog.Log.Logger.Error(ex, "BackgroundServiceTask");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }


            }
        }
        protected async Task WorkContext(IServiceProvider serviceProvider, ScheduleTask scheduleTask)
        {
            var workContext = serviceProvider.GetRequiredService<IWorkContext>();
            var storeHelper = serviceProvider.GetRequiredService<IStoreHelper>();
            await workContext.SetCurrentCustomer();
            await storeHelper.SetCurrentStore(scheduleTask.StoreId);
        }
    }
}
