using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

public class MigrationUniqueOrderNumberIndex : IMigration
{
    private const string IndexName = "OrderNumber";

    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("F4D2A7C1-5E93-4B08-9A6D-3C71E852B4F0");
    public string Name => "Make the index on Order.OrderNumber unique";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Order>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUniqueOrderNumberIndex>>();

        try
        {
            var duplicates = repository.Table.Select(x => x.OrderNumber).ToList()
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicates.Count != 0)
            {
                //the upgrade must not stop because of data that is already inconsistent - report and move on,
                //the index can be created by hand once the numbers are corrected
                logService.LogError(
                    "The unique index on Order.OrderNumber was not created - the following order numbers are used by more than one order: {OrderNumbers}. Correct them and create the index manually",
                    string.Join(", ", duplicates));
                return true;
            }

            //the non-unique index of the same name is dropped first - the driver rejects a redefinition
            databaseContext.DeleteIndex(repository, IndexName).GetAwaiter().GetResult();
            databaseContext.CreateIndex(repository,
                OrderBuilder<Order>.Create().Descending(x => x.OrderNumber), IndexName, true)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - UniqueOrderNumberIndex (2.4)");
        }

        return true;
    }
}
