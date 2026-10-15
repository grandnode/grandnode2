using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Data;
using Grand.Domain.Tasks;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Task service
/// </summary>
public class ScheduleTaskService : IScheduleTaskService
{
    #region Fields

    private readonly IRepository<ScheduleTask> _taskRepository;

    #endregion

    #region Ctor

    public ScheduleTaskService(IRepository<ScheduleTask> taskRepository)
    {
        _taskRepository = taskRepository;
    }

    #endregion

    /// <summary>
    ///     Gets a task
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <returns>Task</returns>
    public virtual Task<ScheduleTask> GetTaskById(string taskId)
    {
        return _taskRepository.GetByIdAsync(taskId);
    }

    /// <summary>
    ///     Gets a task by its name
    /// </summary>
    /// <returns>Task</returns>
    public virtual async Task<ScheduleTask> GetTaskByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var task = _taskRepository.Table.FirstOrDefault(x => x.ScheduleTaskName == name);
        return await Task.FromResult(task);
    }

    /// <summary>
    ///     Gets all tasks
    /// </summary>
    /// <returns>Tasks</returns>
    public virtual async Task<IList<ScheduleTask>> GetAllTasks()
    {
        return await _taskRepository.ToListAsync(_taskRepository.Table);
    }

    /// <summary>
    ///     Insert the task
    /// </summary>
    /// <param name="task">Task</param>
    public virtual async Task<ScheduleTask> InsertTask(ScheduleTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        return await _taskRepository.InsertAsync(task);
    }

    /// <summary>
    ///     Updates the task
    /// </summary>
    /// <param name="task">Task</param>
    public virtual async Task UpdateTask(ScheduleTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        await _taskRepository.UpdateAsync(task);
    }

    /// <summary>
    ///     Atomically claims a single run of the task (compare-and-set on LastStartUtc,
    ///     refused while the lease of a previous run has not run out)
    /// </summary>
    public virtual async Task<bool> TryClaimTaskRun(string taskId, DateTime? expectedLastStartUtc,
        DateTime runStartUtc, DateTime leasedUntilUtc, string instanceId)
    {
        //truncate to milliseconds so the value survives the database round-trip unchanged
        runStartUtc = TruncateToMilliseconds(runStartUtc);

        //conditional update - matches only when no other instance has claimed the run
        //in the meantime; on MongoDB a single update is atomic, so exactly one instance wins
        await _taskRepository.UpdateOneAsync(
            x => x.Id == taskId && x.LastStartUtc == expectedLastStartUtc &&
                 (x.LeasedUntilUtc == null || x.LeasedUntilUtc < runStartUtc),
            UpdateBuilder<ScheduleTask>.Create()
                .Set(x => x.LastStartUtc, runStartUtc)
                .Set(x => x.LeasedUntilUtc, TruncateToMilliseconds(leasedUntilUtc))
                .Set(x => x.LeasedByInstance, instanceId));

        //UpdateOneAsync does not report whether the filter matched - read back the winner.
        //LiteDB returns dates as local time, so compare in UTC - otherwise no claim ever
        //succeeds on a host outside the UTC time zone
        var task = await GetTaskById(taskId);
        return task != null && task.LeasedByInstance == instanceId &&
               task.LastStartUtc?.ToUniversalTime() == runStartUtc;
    }

    /// <summary>
    ///     Extends the lease of a run, only while the given instance still holds it
    /// </summary>
    public virtual async Task RenewTaskLease(string taskId, string instanceId, DateTime leasedUntilUtc)
    {
        await _taskRepository.UpdateOneAsync(
            x => x.Id == taskId && x.LeasedByInstance == instanceId && x.LeasedUntilUtc != null,
            UpdateBuilder<ScheduleTask>.Create()
                .Set(x => x.LeasedUntilUtc, TruncateToMilliseconds(leasedUntilUtc)));
    }

    /// <summary>
    ///     Records the result of a claimed run and releases its lease, only while the given instance still holds it
    /// </summary>
    public virtual async Task CompleteTaskRun(string taskId, string instanceId, bool succeeded, DateTime endUtc,
        bool disableTask)
    {
        var update = UpdateBuilder<ScheduleTask>.Create()
            .Set(x => x.LeasedUntilUtc, null);
        if (succeeded)
            update.Set(x => x.LastSuccessUtc, TruncateToMilliseconds(endUtc))
                .Set(x => x.LastNonSuccessEndUtc, null);
        else
            update.Set(x => x.LastNonSuccessEndUtc, TruncateToMilliseconds(endUtc));
        if (disableTask)
            update.Set(x => x.Enabled, false);

        //LeasedByInstance changes only with a new claim, so a match means no other instance has
        //taken the task over - runs of one task on one instance never overlap
        await _taskRepository.UpdateOneAsync(x => x.Id == taskId && x.LeasedByInstance == instanceId, update);
    }

    private static DateTime TruncateToMilliseconds(DateTime value)
    {
        return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }

    /// <summary>
    ///     Delete the task
    /// </summary>
    /// <param name="task">Task</param>
    public virtual async Task DeleteTask(ScheduleTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        await _taskRepository.DeleteAsync(task);
    }
}