using Grand.Business.Core.Interfaces.Common.Audit;
using Grand.Business.Core.Interfaces.System.ScheduleTasks;
using Grand.Infrastructure.Configuration;

namespace Grand.Module.ScheduledTasks.BackgroundServices;

/// <summary>
///     Deletes audit entries older than the retention period - the only way entries leave the log
/// </summary>
public class AuditLogRetentionScheduleTask(IAuditService auditService, AuditConfig auditConfig) : IScheduleTask
{
    public async Task Execute(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (auditConfig.RetentionDays <= 0)
            return;

        await auditService.DeleteOlderThan(DateTime.UtcNow.AddDays(-auditConfig.RetentionDays));
    }
}
