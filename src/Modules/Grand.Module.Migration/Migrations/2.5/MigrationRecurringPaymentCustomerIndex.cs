using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationRecurringPaymentCustomerIndex : IMigration
{
    private const string IndexName = "CustomerId_StoreId_CreatedOnUtc";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("5b0c2f3e-8d4a-4e61-9a7f-2c1e6d8b3f04");
    public string Name => "Customer index on Recurring payment 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<RecurringPayment>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRecurringPaymentCustomerIndex>>();

        try
        {
            //the My account tab lists a customer's recurring payments in a store, newest first
            await databaseContext.CreateIndex(repository,
                OrderBuilder<RecurringPayment>.Create().Ascending(x => x.CustomerId).Ascending(x => x.StoreId)
                    .Descending(x => x.CreatedOnUtc), IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RecurringPaymentCustomerIndex (2.5)");
            return false;
        }

        return true;
    }
}
