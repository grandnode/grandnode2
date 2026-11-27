using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationRecurringPaymentCreatedOnIndex : IMigration
{
    private const string IndexName = "CreatedOnUtc";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("2571362b-ae02-4118-b14f-eb91d15cd91f");
    public string Name => "Created on index on Recurring payment 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<RecurringPayment>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRecurringPaymentCreatedOnIndex>>();

        try
        {
            //the Admin and Store panels list recurring payments newest first
            await databaseContext.CreateIndex(repository,
                OrderBuilder<RecurringPayment>.Create().Descending(x => x.CreatedOnUtc), IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RecurringPaymentCreatedOnIndex (2.5)");
            return false;
        }

        return true;
    }
}
