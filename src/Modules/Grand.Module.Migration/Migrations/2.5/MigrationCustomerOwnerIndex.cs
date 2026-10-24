using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationCustomerOwnerIndex : IMigration
{
    private const string IndexName = "OwnerId_1";

    public int Priority => 1;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("6f0d4b9e-3c1a-4e27-9a85-2b7e4c1d9f63");
    public string Name => "Index on Customer owner 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Customer>>();
        var databaseContext = serviceProvider.GetRequiredService<IDatabaseContext>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationCustomerOwnerIndex>>();

        try
        {
            //an owner's sub-accounts are listed on the storefront and in the admin customer form, and the admin
            //checks for them before linking an owner - each a scan of the whole customer collection without it
            await databaseContext.CreateIndex(repository,
                OrderBuilder<Customer>.Create().Ascending(x => x.OwnerId), IndexName);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - CustomerOwnerIndex (2.5)");
            return false;
        }

        return true;
    }
}
