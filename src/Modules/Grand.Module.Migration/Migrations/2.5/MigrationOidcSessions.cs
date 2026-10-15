using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationOidcSessions : IMigration
{
    private const string TaskName = "Delete OpenID Connect sessions";

    public int Priority => 2;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("3f6a9e21-7c4b-4d0e-9b8a-5e2c1d7f4a63");
    public string Name => "OpenID Connect sessions: indexes and cleanup task";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var sessionRepository = serviceProvider.GetRequiredService<IRepository<OidcSession>>();
        var taskRepository = serviceProvider.GetRequiredService<IRepository<ScheduleTask>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationOidcSessions>>();
        var success = true;

        try
        {
            databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.Sid), "Sid").GetAwaiter().GetResult();
            databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.Subject), "Subject").GetAwaiter().GetResult();
            databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.CustomerId), "CustomerId").GetAwaiter().GetResult();
            databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.EndedUtc), "EndedUtc").GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OidcSessions indexes (2.5)");
            success = false;
        }

        try
        {
            if (!taskRepository.Table.Any(x => x.ScheduleTaskName == TaskName))
                taskRepository.Insert(new ScheduleTask {
                    ScheduleTaskName = TaskName, Enabled = true, StopOnError = false, TimeInterval = 1440
                });
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OidcSessions schedule task (2.5)");
        }

        return success;
    }
}
