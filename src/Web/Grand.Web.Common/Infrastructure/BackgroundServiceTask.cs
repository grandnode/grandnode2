using System.Runtime.CompilerServices;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Tasks;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
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

    //how long a claimed run blocks other instances; the running instance renews it while the
    //task executes, so it only runs out when that instance dies without finishing the run
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
    internal static readonly TimeSpan LeaseRenewInterval = TimeSpan.FromMinutes(1);

    internal enum ScheduleAction
    {
        //task missing, disabled, pinned to another instance, or still running elsewhere -
        //all reversible, poll again later
        Retry,
        //enabled, due, and not running elsewhere - claim and execute
        RunNow,
        //enabled, but not due yet - sleep until it is
        WaitForNextRun
    }

    internal readonly record struct ScheduleDecision(ScheduleAction Action, TimeSpan Delay);

    /// <summary>
    /// Pure decision of what the loop should do this iteration and how long to sleep
    /// afterward. Deliberately has no side effects and no terminal outcome - every branch
    /// resolves to a delay-and-retry, never to "stop the loop" - so the reversible states
    /// (task not seeded yet, disabled, leased elsewhere) can never end the loop for good.
    /// </summary>
    /// <param name="task">Task as read from the database</param>
    /// <param name="machineName">Name of the machine (container hostname, pod name)</param>
    /// <param name="instanceName">Configured Application:InstanceName, empty when not set</param>
    /// <param name="utcNow">Current time</param>
    internal static ScheduleDecision Decide(ScheduleTask task, string machineName, string instanceName,
        DateTime utcNow)
    {
        if (task == null)
            return new ScheduleDecision(ScheduleAction.Retry, TimeSpan.FromMinutes(1));

        var timeInterval = TimeSpan.FromMinutes(task.TimeInterval > 0 ? task.TimeInterval : 1);

        if (!task.Enabled || !IsPinnedToThisInstance(task.LeasedByMachineName, machineName, instanceName))
            return new ScheduleDecision(ScheduleAction.Retry, timeInterval);

        //LiteDB returns dates as local time - compared with utcNow, they must be UTC
        if (task.LastStartUtc.HasValue)
        {
            var nextRunUtc = task.LastStartUtc.Value.ToUniversalTime().AddMinutes(task.TimeInterval);
            //the exact remaining time, not whole minutes - truncating it turned the last
            //minute before every run into a delay-free loop of database reads
            if (utcNow < nextRunUtc)
                return new ScheduleDecision(ScheduleAction.WaitForNextRun, nextRunUtc - utcNow);
        }

        //the previous run is still executing on another instance - come back when its lease runs out
        var leasedUntilUtc = task.LeasedUntilUtc?.ToUniversalTime();
        if (leasedUntilUtc > utcNow)
            return new ScheduleDecision(ScheduleAction.Retry, leasedUntilUtc.Value - utcNow);

        return new ScheduleDecision(ScheduleAction.RunNow, timeInterval);
    }

    /// <summary>
    /// A task pinned to a machine runs only on the instance with that name. Container hostnames
    /// change on every deployment, so the configured instance name is accepted too - and it can be
    /// shared by several instances, which then compete for the run like unpinned ones.
    /// </summary>
    private static bool IsPinnedToThisInstance(string leasedByMachineName, string machineName, string instanceName)
    {
        if (string.IsNullOrEmpty(leasedByMachineName))
            return true;

        return string.Equals(leasedByMachineName, machineName, StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(instanceName) &&
                string.Equals(leasedByMachineName, instanceName, StringComparison.OrdinalIgnoreCase));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //resolved once from the root provider (not the per-iteration scope) so it also
        //survives to log failures raised before/around a scope, e.g. in the outer catch
        var logger = _serviceProvider.GetService<ILogger<BackgroundServiceTask>>();
        var instanceName = _serviceProvider.GetService<AppConfig>()?.InstanceName;

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var serviceProvider = scope.ServiceProvider;
                var scheduleTaskService = serviceProvider.GetService<IScheduleTaskService>();
                var task = await scheduleTaskService.GetTaskByName(Name);
                var decision = Decide(task, Environment.MachineName, instanceName, DateTime.UtcNow);

                if (task == null)
                    logger.LogInformation("Task {TaskName} is not exists in the database", Name);
                else if (decision.Action != ScheduleAction.Retry)
                    await RunTask(serviceProvider, scheduleTaskService, task,
                        decision.Action == ScheduleAction.RunNow, logger, stoppingToken);

                //every branch above falls through to here - the loop always sleeps and
                //retries, it never exits on its own (only cancellation ends it)
                await Task.Delay(decision.Delay, stoppingToken);
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
            if (!due)
                return;

            //claim this run atomically - when several instances race,
            //only one wins and executes the task (no duplicated e-mails etc.)
            var runStartUtc = DateTime.UtcNow;
            var claimed = await scheduleTaskService.TryClaimTaskRun(task.Id, task.LastStartUtc,
                runStartUtc, runStartUtc.Add(LeaseDuration), InstanceId);
            if (claimed)
            {
                updateTask = true;
                task.LastStartUtc = runStartUtc;
                task.LeasedByInstance = InstanceId;

                //keeps other instances off the task while it runs, even past its next due time
                using var leaseCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var leaseRenewal = RenewLease(task.Id, logger, leaseCts.Token);
                try
                {
                    //assign current customer (background task) / current store (from task).
                    //Set in this frame, not in an awaited helper: AsyncLocal values written inside
                    //an async method are discarded when it returns, so Execute would see null
                    var contextAccessor = serviceProvider.GetRequiredService<IContextAccessor>();
                    var (storeContext, workContext) = await InitializeContext(serviceProvider, task);
                    contextAccessor.StoreContext = storeContext;
                    contextAccessor.WorkContext = workContext;

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
                finally
                {
                    await leaseCts.CancelAsync();
                    await leaseRenewal;
                }

                //the run is over - the write below releases the lease for the next one
                task.LeasedUntilUtc = null;
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

    /// <summary>
    /// Extends the lease of the claimed run until cancelled. Uses its own scope: it runs alongside
    /// the task, which keeps using the iteration's scope. A failed renewal is only logged - the
    /// lease is several renewals long, and the run itself must not fail because of it.
    /// </summary>
    private async Task RenewLease(string taskId, ILogger logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                await Task.Delay(LeaseRenewInterval, cancellationToken);

                using var scope = _serviceProvider.CreateScope();
                var scheduleTaskService = scope.ServiceProvider.GetRequiredService<IScheduleTaskService>();
                await scheduleTaskService.RenewTaskLease(taskId, InstanceId, DateTime.UtcNow.Add(LeaseDuration));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                //the run has ended
            }
            catch (Exception exc)
            {
                logger.LogWarning(exc, "Could not renew the lease of task {TaskName}", Name);
            }
    }

    private static async Task<(IStoreContext storeContext, IWorkContext workContext)> InitializeContext(
        IServiceProvider serviceProvider, ScheduleTask scheduleTask)
    {
        var storeContextSetter = serviceProvider.GetRequiredService<IStoreContextSetter>();
        var storeContext = await storeContextSetter.InitializeStoreContext(scheduleTask.StoreId);

        var workContextSetter = serviceProvider.GetRequiredService<IWorkContextSetter>();
        var workContext = await workContextSetter.InitializeWorkContext(scheduleTask.StoreId);

        return (storeContext, workContext);
    }
}