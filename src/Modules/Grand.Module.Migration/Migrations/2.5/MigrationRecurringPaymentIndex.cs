using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationRecurringPaymentIndex : IMigration
{
    private const string IndexName = "InitialOrderId";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("eada9902-2f77-483a-b70b-42ca679ee016");
    public string Name => "Index on Recurring payment 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<RecurringPayment>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRecurringPaymentIndex>>();

        try
        {
            //every order screen and every provider webhook finds the recurring payment by the order the customer placed;
            //unique, so an order never has two
            await databaseContext.CreateIndex(repository,
                OrderBuilder<RecurringPayment>.Create().Ascending(x => x.InitialOrderId), IndexName, true);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RecurringPaymentIndex (2.5)");
            return false;
        }

        return true;
    }
}
