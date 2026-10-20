using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationProductTextIndex : IMigration
{
    public int Priority => 3;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("35f3752a-638b-4219-8dad-fbb976f867db");
    public string Name => "Product text index for keyword search, when DatabaseConfig.UseProductTextSearch is on";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        //switched on later, the index is created by hand - see UseProductTextSearch in appsettings.json
        if (!serviceProvider.GetRequiredService<DatabaseConfig>().UseProductTextSearch)
            return true;

        var repository = serviceProvider.GetRequiredService<IRepository<Product>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationProductTextIndex>>();

        try
        {
            //a no-op on LiteDB
            await databaseContext.CreateTextIndex(repository, ProductTextIndex.Fields, ProductTextIndex.Name);
        }
        catch (Exception ex)
        {
            //keyword search scans as before without the index
            logService.LogError(ex, "UpgradeProcess - ProductTextIndex (2.5)");
            return false;
        }

        return true;
    }
}
