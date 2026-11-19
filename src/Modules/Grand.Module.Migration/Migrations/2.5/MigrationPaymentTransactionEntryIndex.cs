using Grand.Data;
using Grand.Domain.Payments;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationPaymentTransactionEntryIndex : IMigration
{
    private const string IndexName = "PaymentTransactionId_CreatedOnUtc";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("3914fc08-fc5f-4f06-a737-a01bf2cb4b2a");
    public string Name => "Index on Payment transaction history 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<PaymentTransactionEntry>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationPaymentTransactionEntryIndex>>();

        try
        {
            //a transaction's history is read on its screen, a scan of every payment operation of the shop without it
            await databaseContext.CreateIndex(repository,
                OrderBuilder<PaymentTransactionEntry>.Create().Ascending(x => x.PaymentTransactionId)
                    .Ascending(x => x.CreatedOnUtc), IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - PaymentTransactionEntryIndex (2.5)");
            return false;
        }

        return true;
    }
}
