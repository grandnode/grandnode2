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

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var sessionRepository = serviceProvider.GetRequiredService<IRepository<OidcSession>>();
        var taskRepository = serviceProvider.GetRequiredService<IRepository<ScheduleTask>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationOidcSessions>>();
        var success = true;

        try
        {
            await databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.Sid), "Sid");
            await databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.Subject), "Subject");
            await databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.CustomerId), "CustomerId");
            await databaseContext.CreateIndex(sessionRepository,
                OrderBuilder<OidcSession>.Create().Ascending(x => x.EndedUtc), "EndedUtc");
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OidcSessions indexes (2.5)");
            success = false;
        }

        try
        {
            if (!await taskRepository.AnyAsync(taskRepository.Table.Where(x => x.ScheduleTaskName == TaskName)))
                await taskRepository.InsertAsync(new ScheduleTask {
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
