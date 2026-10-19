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
    ///     Updates the task, except its lease (LeasedUntilUtc, LeasedByInstance) - that is written only by
    ///     claiming, renewing and completing a run
    /// </summary>
    /// <param name="task">Task</param>
    Task UpdateTask(ScheduleTask task);

    /// <summary>
    ///     Atomically claims a single run of the task, so that only one application instance
    ///     executes it. The claim succeeds only if LastStartUtc still has the value the caller
    ///     read (compare-and-set) - a concurrent claim by another instance makes it fail.
    ///     It also fails while LeasedUntilUtc of a previous run is still in the future,
    ///     so a run that outlasts the interval is not started a second time elsewhere.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="expectedLastStartUtc">LastStartUtc value read before the claim</param>
    /// <param name="runStartUtc">New LastStartUtc value (start of the claimed run)</param>
    /// <param name="leasedUntilUtc">Until when the claimed run blocks other instances, unless renewed</param>
    /// <param name="instanceId">Unique identifier of the claiming application instance</param>
    /// <returns>true if this instance won the claim and should execute the task</returns>
    Task<bool> TryClaimTaskRun(string taskId, DateTime? expectedLastStartUtc, DateTime runStartUtc,
        DateTime leasedUntilUtc, string instanceId);

    /// <summary>
    ///     Atomically claims a single run of the task without a lease - the run blocks other
    ///     instances only until its interval elapses, as before leases were introduced.
    /// </summary>
    [Obsolete("Use the overload with leasedUntilUtc - a run claimed without a lease is started again elsewhere once its interval elapses, even if it is still running")]
    Task<bool> TryClaimTaskRun(string taskId, DateTime? expectedLastStartUtc, DateTime runStartUtc,
        string instanceId)
    {
        return TryClaimTaskRun(taskId, expectedLastStartUtc, runStartUtc, runStartUtc, instanceId);
    }

    /// <summary>
    ///     Extends the lease of a running task. Does nothing when the instance no longer holds
    ///     the lease (released, or taken over by another instance after it ran out).
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="instanceId">Unique identifier of the application instance running the task</param>
    /// <param name="leasedUntilUtc">New end of the lease</param>
    /// <returns>False when the instance no longer holds the lease - its run should stop</returns>
    Task<bool> RenewTaskLease(string taskId, string instanceId, DateTime leasedUntilUtc);

    /// <summary>
    ///     Records the result of a claimed run and releases its lease. Writes only the result fields,
    ///     and only while the instance still holds the lease - so it neither reverts changes saved in
    ///     the admin during the run, nor overwrites the claim of an instance that took the task over
    ///     after the lease ran out.
    /// </summary>
    /// <param name="taskId">Task identifier</param>
    /// <param name="instanceId">Unique identifier of the application instance that ran the task</param>
    /// <param name="succeeded">Whether the run completed without an error</param>
    /// <param name="endUtc">End of the run</param>
    /// <param name="disableTask">Disables the task (a failed run of a task that stops on error)</param>
    Task CompleteTaskRun(string taskId, string instanceId, bool succeeded, DateTime endUtc, bool disableTask);

    /// <summary>
    ///     Delete the task
    /// </summary>
    /// <param name="task">Task</param>
    Task DeleteTask(ScheduleTask task);
}