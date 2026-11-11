using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationMerchandiseReturnOrderIndex : IMigration
{
    private const string IndexName = "OrderId_1";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("4c8e1f2a-9d73-4b56-a0e4-6f1b3c7d2e95");
    public string Name => "Index on Merchandise return order 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<MerchandiseReturn>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationMerchandiseReturnOrderIndex>>();

        try
        {
            //an order's returns are read to work out what is left to return - on its page, on the customer's order
            //list and on every return saved - each a scan of the whole returns collection without it
            await databaseContext.CreateIndex(repository,
                OrderBuilder<MerchandiseReturn>.Create().Ascending(x => x.OrderId), IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MerchandiseReturnOrderIndex (2.5)");
            return false;
        }

        return true;
    }
}
