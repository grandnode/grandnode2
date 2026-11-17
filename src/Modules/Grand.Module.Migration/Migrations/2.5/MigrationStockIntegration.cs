using Grand.Data;
using Grand.Data.Inventory;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     Stock integration: the indexes of the idempotency and SKU lookups, and the permission that lets a technical ERP
///     account write and read stock without catalog editing, granted to administrators
/// </summary>
public class MigrationStockIntegration : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("2c7a34b0-8309-4398-9bbf-38bda11b2a1e");
    public string Name => "Stock integration: indexes and permission (2.5)";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        try
        {
            await InventoryIndexes.CreateIntegrationIndexes(serviceProvider.GetRequiredService<IDatabaseContext>(),
                serviceProvider.GetRequiredService<IRepository<InventoryJournal>>(),
                serviceProvider.GetRequiredService<IRepository<Product>>());
            await AddPermission(serviceProvider);
            return true;
        }
        catch (Exception ex)
        {
            serviceProvider.GetRequiredService<ILogger<MigrationStockIntegration>>()
                .LogError(ex, "UpgradeProcess - Stock integration (2.5)");
            return false;
        }
    }

    private static async Task AddPermission(IServiceProvider serviceProvider)
    {
        var permissionRepository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var groupRepository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();

        if (await permissionRepository.AnyAsync(
                permissionRepository.Table.Where(x => x.SystemName == PermissionSystemName.StockIntegration)))
            return;

        var permission = new Permission {
            Name = StandardPermission.ManageStockIntegration.Name,
            SystemName = StandardPermission.ManageStockIntegration.SystemName,
            Area = StandardPermission.ManageStockIntegration.Area,
            Category = StandardPermission.ManageStockIntegration.Category,
            Actions = StandardPermission.ManageStockIntegration.Actions.ToList()
        };
        var administrators = await groupRepository.FirstOrDefaultAsync(
            groupRepository.Table.Where(x => x.SystemName == SystemCustomerGroupNames.Administrators));
        if (administrators != null)
            permission.CustomerGroups.Add(administrators.Id);

        await permissionRepository.InsertAsync(permission);
    }
}
