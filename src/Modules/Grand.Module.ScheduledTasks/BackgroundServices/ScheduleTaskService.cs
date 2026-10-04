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
    ///     Atomically claims a single run of the task (compare-and-set on LastStartUtc)
    /// </summary>
    public virtual async Task<bool> TryClaimTaskRun(string taskId, DateTime? expectedLastStartUtc,
        DateTime runStartUtc, string instanceId)
    {
        //truncate to milliseconds so the value survives the database round-trip unchanged
        runStartUtc = new DateTime(runStartUtc.Ticks - runStartUtc.Ticks % TimeSpan.TicksPerMillisecond,
            DateTimeKind.Utc);

        //conditional update - matches only when no other instance has claimed the run
        //in the meantime; on MongoDB a single update is atomic, so exactly one instance wins
        await _taskRepository.UpdateOneAsync(
            x => x.Id == taskId && x.LastStartUtc == expectedLastStartUtc,
            UpdateBuilder<ScheduleTask>.Create()
                .Set(x => x.LastStartUtc, runStartUtc)
                .Set(x => x.LeasedByInstance, instanceId));

        //UpdateOneAsync does not report whether the filter matched - read back the winner
        var task = await GetTaskById(taskId);
        return task != null && task.LeasedByInstance == instanceId && task.LastStartUtc == runStartUtc;
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