using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._2
{
    public class MigrationSystemPermission : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(2, 2);
        public Guid Identity => new("BFBAEB25-288A-4114-BC0D-0272B57B1725");
        public string Name => "Install new permission - ManageAccessVendorPanel";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IRepository<Permission>>();
            var groupService = serviceProvider.GetRequiredService<IGroupService>();
            var logService = serviceProvider.GetRequiredService<ILogger>();

            try
            {
                var customerGroupVendorId = groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Vendors).GetAwaiter().GetResult().Id;
                var permissionAccessVendor = repository.Table.FirstOrDefault(x => x.SystemName == PermissionSystemName.AccessVendorPanel);
                if (permissionAccessVendor == null)
                {
                    permissionAccessVendor = StandardPermission.ManageAccessVendorPanel;
                    permissionAccessVendor.CustomerGroups.Add(customerGroupVendorId);
                    repository.Insert(permissionAccessVendor);
                }
                var permissionAccessAdmin = repository.Table.FirstOrDefault(x => x.SystemName == PermissionSystemName.AccessAdminPanel);
                if (permissionAccessAdmin != null)
                {
                    permissionAccessAdmin.CustomerGroups.Remove(customerGroupVendorId);
                    repository.Update(permissionAccessAdmin);
                }
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - add new permission", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }
    }
}
