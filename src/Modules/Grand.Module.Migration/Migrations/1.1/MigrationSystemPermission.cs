using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._1._1;

public class MigrationSystemPermission : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(1, 1);
    public Guid Identity => new("361F2D87-D067-44FE-B0A6-0817B201730A");
    public string Name => "Install new permission - System";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<Permission>>();
        var customerGroupRepository = serviceProvider.GetRequiredService<IRepository<CustomerGroup>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationSystemPermission>>();
        var administrator = customerGroupRepository.Table.FirstOrDefault(x => x.SystemName == SystemCustomerGroupNames.Administrators);
        try
        {
            var permission = repository.Table.FirstOrDefault(x => x.SystemName == PermissionSystemName.System);
            if (permission == null)
            {
                permission = new Permission {
                    Name = "Manage System",
                    SystemName = PermissionSystemName.System,
                    Area = "Admin area",
                    Category = "System"
                };
                permission.CustomerGroups.Add(administrator!.Id);
                repository.Insert(permission);
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - UpdateAdminSiteMap");
        }

        return true;
    }
}