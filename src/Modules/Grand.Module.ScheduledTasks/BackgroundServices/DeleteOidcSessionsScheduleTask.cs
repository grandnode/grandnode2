using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Deletes the records of ended OpenID Connect sessions and of sessions past their lifetime
/// </summary>
public class DeleteOidcSessionsScheduleTask(IOidcSessionService oidcSessionService) : IScheduleTask
{
    public Task Execute() => oidcSessionService.DeleteExpired();
}
