using Grand.Domain.Tasks;

namespace Grand.Business.Core.Interfaces.System.ScheduleTasks;

public interface IScheduleTaskService
{
    /// <summary>
    ///     Gets a task
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <returns>Task</returns>
    Task<ScheduleTask> GetTaskById(string taskId);

    /// <summary>
    ///     Gets a task by its type
    /// </summary>
    /// <param name="name">Task name</param>
    /// <returns>Task</returns>
    Task<ScheduleTask> GetTaskByName(string name);

    /// <summary>
    ///     Gets all tasks
    /// </summary>
    /// <returns>Tasks</returns>
    Task<IList<ScheduleTask>> GetAllTasks();

    /// <summary>
    ///     Insert the task
    /// </summary>
    /// <param name="task">Task</param>
    Task<ScheduleTask> InsertTask(ScheduleTask task);

    /// <summary>
    ///     Updates the task
    /// </summary>
    /// <param name="task">Task</param>
    Task UpdateTask(ScheduleTask task);

    /// <summary>
    ///     Atomically claims a single run of the task, so that only one application instance
    ///     executes it. The claim succeeds only if LastStartUtc still has the value the caller
    ///     read (compare-and-set) - a concurrent claim by another instance makes it fail.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="expectedLastStartUtc">LastStartUtc value read before the claim</param>
    /// <param name="runStartUtc">New LastStartUtc value (start of the claimed run)</param>
    /// <param name="instanceId">Unique identifier of the claiming application instance</param>
    /// <returns>true if this instance won the claim and should execute the task</returns>
    Task<bool> TryClaimTaskRun(string taskId, DateTime? expectedLastStartUtc, DateTime runStartUtc, string instanceId);

    /// <summary>
    ///     Delete the task
    /// </summary>
    /// <param name="task">Task</param>
    Task DeleteTask(ScheduleTask task);
}