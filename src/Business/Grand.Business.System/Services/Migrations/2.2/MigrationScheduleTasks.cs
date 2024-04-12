using Grand.Data;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._2;

public class MigrationScheduleTasks : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 2);
    public Guid Identity => new("17E09B6B-A8BB-45B4-B13D-4D76711DCD7A");
    public string Name => "Remove ScheduleTask - Clear log";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<ScheduleTask>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationScheduleTasks>>();
        try
        {
            var scheduleTask = repository.Table.FirstOrDefault(x => x.ScheduleTaskName == "Clear log");
            if (scheduleTask != null)
                repository.Delete(scheduleTask);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - Remove ScheduleTasks");
        }

        return true;
    }
}