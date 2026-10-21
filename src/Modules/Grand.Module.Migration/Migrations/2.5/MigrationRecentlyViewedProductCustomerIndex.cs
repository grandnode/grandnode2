using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationRecentlyViewedProductCustomerIndex : IMigration
{
    private const string IndexName = "CustomerId.CreatedOnUtc";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("d356a390-bb1b-48e6-b1a1-ba5f6b1aea32");
    public string Name => "Index on RecentlyViewedProduct customer + view date 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<RecentlyViewedProduct>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRecentlyViewedProductCustomerIndex>>();

        try
        {
            //serves the customer's list in its display order - the CustomerId.ProductId index puts ProductId between
            //the two, so the list was sorted in memory on every product view
            await databaseContext.CreateIndex(repository,
                OrderBuilder<RecentlyViewedProduct>.Create().Ascending(x => x.CustomerId)
                    .Descending(x => x.CreatedOnUtc).Descending(x => x.Id), IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - RecentlyViewedProductCustomerIndex (2.5)");
            return false;
        }

        return true;
    }
}
