using System.Runtime.CompilerServices;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Tasks;
using Grand.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Grand.Web.Common.Tests")]

namespace Grand.Web.Common.Infrastructure;

public class BackgroundServiceTask : BackgroundService
{
    //unique per process - pod/machine names are not stable across scaling, so a random
    //id generated at startup is all that is needed to tell instances apart
    private static readonly string InstanceId = Guid.NewGuid().ToString("N");

    private readonly IServiceProvider _serviceProvider;
    private readonly string Name;

    public BackgroundServiceTask(string name, IServiceProvider serviceProvider)
    {
        Name = name;
        _serviceProvider = serviceProvider;
    }

    internal enum ScheduleAction
    {
        //task missing, disabled, or leased to another instance - all reversible, poll again later
        Retry,
        //own lease (or none), enabled, and due - claim and execute
        RunNow,
        //own lease, enabled, but not due yet - sleep until it is
        WaitForNextRun
    }

    internal readonly record struct ScheduleDecision(ScheduleAction Action, int DelayMinutes);

    /// <summary>
    /// Pure decision of what the loop should do this iteration and how long to sleep
    /// afterward. Deliberately has no side effects and no terminal outcome - every branch
    /// resolves to a delay-and-retry, never to "stop the loop" - so the reversible states
    /// (task not seeded yet, disabled, leased elsewhere) can never end the loop for good.
    /// </summary>
    internal static ScheduleDecision Decide(ScheduleTask task, string machineName, DateTime utcNow)
    {
        if (task == null)
            return new ScheduleDecision(ScheduleAction.Retry, 1);

        var timeInterval = task.TimeInterval > 0 ? task.TimeInterval : 1;

        var eligible = task.Enabled &&
                       (string.IsNullOrEmpty(task.LeasedByMachineName) || machineName == task.LeasedByMachineName);
        if (!eligible)
            return new ScheduleDecision(ScheduleAction.Retry, timeInterval);

        if (task.LastStartUtc.HasValue)
        {
            var nextRunUtc = task.LastStartUtc.Value.AddMinutes(task.TimeInterval);
            if (utcNow < nextRunUtc)
                return new ScheduleDecision(ScheduleAction.WaitForNextRun, (int)(nextRunUtc - utcNow).TotalMinutes);
        }

        return new ScheduleDecision(ScheduleAction.RunNow, timeInterval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //resolved once from the root provider (not the per-iteration scope) so it also
        //survives to log failures raised before/around a scope, e.g. in the outer catch
        var logger = _serviceProvider.GetService<ILogger<BackgroundServiceTask>>();

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var serviceProvider = scope.ServiceProvider;
                var scheduleTaskService = serviceProvider.GetService<IScheduleTaskService>();
                var task = await scheduleTaskService.GetTaskByName(Name);
                var decision = Decide(task, Environment.MachineName, DateTime.UtcNow);

                if (task == null)
                    logger.LogInformation("Task {TaskName} is not exists in the database", Name);
                else if (decision.Action != ScheduleAction.Retry)
                    await RunTask(serviceProvider, scheduleTaskService, task,
                        decision.Action == ScheduleAction.RunNow, logger, stoppingToken);

                //every branch above falls through to here - the loop always sleeps and
                //retries, it never exits on its own (only cancellation ends it)
                await Task.Delay(TimeSpan.FromMinutes(decision.DelayMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                //application shutdown - not a task failure
            }
            catch (Exception exc)
            {
                logger.LogError(exc, "Unhandled error in the background loop for task {TaskName}", Name);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
    }

    private async Task RunTask(IServiceProvider serviceProvider, IScheduleTaskService scheduleTaskService,
        ScheduleTask task, bool due, ILogger logger, CancellationToken stoppingToken)
    {
        var updateTask = false;
        var scheduleTask = serviceProvider.GetRequiredKeyedService<IScheduleTask>(task.ScheduleTaskName);
        if (scheduleTask != null)
        {
            //assign current customer (background task) / current store (from task)
            await WorkContext(serviceProvider, task);

            if (!due)
                return;

            //claim this run atomically - when several instances race,
            //only one wins and executes the task (no duplicated e-mails etc.)
            var runStartUtc = DateTime.UtcNow;
            var claimed = await scheduleTaskService.TryClaimTaskRun(task.Id, task.LastStartUtc,
                runStartUtc, InstanceId);
            if (claimed)
            {
                updateTask = true;
                task.LastStartUtc = runStartUtc;
                task.LeasedByInstance = InstanceId;
                try
                {
                    logger.LogInformation("Task {TaskName} execute", Name);
                    await scheduleTask.Execute();
                    task.LastSuccessUtc = DateTime.UtcNow;
                    task.LastNonSuccessEndUtc = null;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    //application shutdown - do not classify as a task failure
                    throw;
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
            else
            {
                //another instance executes this run - check again on the next interval
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("Task {TaskName} claimed by another instance, skipping", Name);
            }
        }
        else
        {
            updateTask = true;
            task.Enabled = !task.StopOnError;
            task.LastNonSuccessEndUtc = DateTime.UtcNow;
            logger.LogError("Type {TaskName} is not registered", Name);
        }

        //persist only when this instance actually ran the task - an unconditional
        //write would overwrite the claim/results of the winning instance with stale data
        if (updateTask)
            await scheduleTaskService.UpdateTask(task);
    }

    private static async Task WorkContext(IServiceProvider serviceProvider, ScheduleTask scheduleTask)
    {
        var contextAccessor = serviceProvider.GetRequiredService<IContextAccessor>();
        
        var storeContext = serviceProvider.GetRequiredService<IStoreContextSetter>();
        contextAccessor.StoreContext = await storeContext.InitializeStoreContext(scheduleTask.StoreId);

        var workContext = serviceProvider.GetRequiredService<IWorkContextSetter>();
        contextAccessor.WorkContext = await workContext.InitializeWorkContext(scheduleTask.StoreId);
    }
}