using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Renames the "Staff" system customer group to "Store manager", matching the earlier rename of
///     <see cref="SystemCustomerGroupNames.StoreManager" /> (previously <c>Staff</c>). Installations that
///     existed before that rename still have a <see cref="CustomerGroup" /> document with
///     SystemName "Staff", seeded by the old installer. Code now looks up the store-manager group by
///     <see cref="SystemCustomerGroupNames.StoreManager" />, so an un-migrated installation silently loses
///     that group. This brings the existing document's SystemName (and, if untouched by an operator, its
///     display Name) in line with the current naming.
/// </summary>
public class MigrationUpdateStoreManagerCustomerGroup : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("3367B869-FA18-4E8F-8721-3FFF82F35D50");
    public string Name => "Rename 'Staff' customer group to 'Store manager' (StoreManager) 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var customerGroupRepository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateStoreManagerCustomerGroup>>();

        try
        {
            var staffGroup = customerGroupRepository.Table.FirstOrDefault(x => x.SystemName == "Staff");
            if (staffGroup == null) return true;

            staffGroup.SystemName = SystemCustomerGroupNames.StoreManager;
            if (staffGroup.Name == "Staff")
                staffGroup.Name = "Store manager";

            customerGroupRepository.Update(staffGroup);
        }
        catch (InvalidOperationException ex)
        {
            logService.LogError(ex, "UpgradeProcess - MigrationUpdateStoreManagerCustomerGroup (2.4)");
        }
        catch (SystemException ex)
        {
            logService.LogError(ex, "UpgradeProcess - MigrationUpdateStoreManagerCustomerGroup (2.4)");
        }

        return true;
    }
}
