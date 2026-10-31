using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationOrderPaymentTerms : IMigration
{
    private const string IndexName = "OwnerId_PaymentDueDateUtc";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("c996c2ad-f2c5-4ca5-b3e8-ff43c9f6a42c");
    public string Name => "Index on Order owner and payment due date 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Order>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationOrderPaymentTerms>>();

        try
        {
            //the trade credit a company has used is the unpaid total of its orders bought on credit, summed on
            //every checkout that offers paying by invoice
            await databaseContext.CreateIndex(repository,
                OrderBuilder<Order>.Create().Ascending(x => x.OwnerId).Ascending(x => x.PaymentDueDateUtc),
                IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - OrderPaymentTerms (2.5)");
            return false;
        }

        return true;
    }
}
