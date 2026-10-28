using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Domain.Audit;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Deletes audit entries older than the retention period - the only way entries leave the log
/// </summary>
public class AuditLogRetentionScheduleTask(IAuditService auditService, AuditSettings auditSettings) : IScheduleTask
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var days = Math.Max(1, auditSettings.RetentionDays);
        await auditService.DeleteOlderThan(DateTime.UtcNow.AddDays(-days));
    }
}
