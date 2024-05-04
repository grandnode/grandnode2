using Grand.Data;
using Grand.Domain.Tasks;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._1;

public class MigrationScheduleTasks : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 1);
    public Guid Identity => new("81E2C287-F5F2-4E52-B0B8-119D8D71D0C2");
    public string Name => "Remove ScheduleTasks";

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
            var scheduleTasks = repository
                .Table.Where(x =>
                    x.ScheduleTaskName == "Customer reminder - AbandonedCart" ||
                    x.ScheduleTaskName == "Customer reminder - RegisteredCustomer" ||
                    x.ScheduleTaskName == "Customer reminder - LastActivity" ||
                    x.ScheduleTaskName == "Customer reminder - LastPurchase" ||
                    x.ScheduleTaskName == "Customer reminder - Birthday" ||
                    x.ScheduleTaskName == "Customer reminder - Completed order" ||
                    x.ScheduleTaskName == "Customer reminder - Unpaid order"
                );
            foreach (var item in scheduleTasks) repository.Delete(item);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RemoveOldScheduleTasks");
        }

        return true;
    }
}