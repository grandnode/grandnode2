using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Deletes the records of ended OpenID Connect sessions and of sessions past their lifetime, and token revocations
///     older than any token they could still refuse
/// </summary>
public class DeleteOidcSessionsScheduleTask(
    IOidcSessionService oidcSessionService,
    IOidcTokenRevocationService oidcTokenRevocationService) : IScheduleTask
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        await oidcSessionService.DeleteExpired();
        await oidcTokenRevocationService.DeleteExpired();
    }
}
