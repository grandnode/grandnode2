using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationSystemPermission : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(1, 1);
        public Guid Identity => new("361F2D87-D067-44FE-B0A6-0817B201730A");
        public string Name => "Install new permission - System";

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
                var permission = repository.Table.FirstOrDefault(x => x.SystemName == PermissionSystemName.System);
                if (permission == null)
                {
                    permission = new Permission {
                        Name = "Manage System",
                        SystemName = PermissionSystemName.System,
                        Area = "Admin area",
                        Category = "System",

                    };
                    permission.CustomerGroups.Add(groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Administrators).GetAwaiter().GetResult().Id);
                    repository.Insert(permission);
                }
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - UpdateAdminSiteMap", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }
    }
}
