using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._2;

public class MigrationSystemPermission : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 2);
    public Guid Identity => new("8F35D0AF-28EF-4734-BC93-9F8A0F63E79A");

    public string Name =>
        "Install new permission - ManageAccessVendorPanel / Delete permission - ManageSystemLog/ManageActivityLog";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var customerGroupRepository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();
        var vendor = await customerGroupRepository.FirstOrDefaultAsync(customerGroupRepository.Table.Where(x => x.SystemName == SystemCustomerGroupNames.Vendors));

        var logService = serviceProvider.GetRequiredService<ILogger<MigrationSystemPermission>>();

        try
        {
            var permissionAccessVendor =
                await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == PermissionSystemName.AccessVendorPanel));
            if (permissionAccessVendor == null)
            {
                permissionAccessVendor = StandardPermission.ManageAccessVendorPanel;
                permissionAccessVendor.CustomerGroups.Add(vendor!.Id);
                await repository.InsertAsync(permissionAccessVendor);
            }

            var permissionAccessAdmin =
                await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == PermissionSystemName.AccessAdminPanel));
            if (permissionAccessAdmin != null)
            {
                permissionAccessAdmin.CustomerGroups.Remove(vendor!.Id);
                await repository.UpdateAsync(permissionAccessAdmin);
            }

            var permissionManageSystemLog = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "ManageSystemLog"));
            if (permissionManageSystemLog != null) await repository.DeleteAsync(permissionManageSystemLog);
            var permissionManageActivityLog = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "ManageActivityLog"));
            if (permissionManageActivityLog != null) await repository.DeleteAsync(permissionManageActivityLog);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - add new permission");
        }

        return true;
    }
}