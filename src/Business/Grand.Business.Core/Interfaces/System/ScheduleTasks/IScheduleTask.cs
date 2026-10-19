namespace Grand.Business.Core.Interfaces.System.ScheduleTasks;

public interface IScheduleTask
{
    /// <summary>
    ///     Executes the task
    /// </summary>
    /// <param name="cancellationToken">
    ///     Cancelled when the application stops or the run loses its lease to another instance.
    ///     Check it between units of work (one e-mail, one order), never inside one.
    /// </param>
    Task Execute(CancellationToken cancellationToken);
}
