using Grand.Domain.Data;
using Grand.Domain.Logging;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationActivityLogTypes : IMigration
    {

        public int Priority => 0;
        public DbVersion Version => new(1, 1);
        public Guid Identity => new("FA12110D-6C60-401F-BA7C-7B94587CA0EC");
        public string Name => "Add missing activity log type for brand collection";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IRepository<ActivityLogType>>();

            if (repository.Table.FirstOrDefault(x => x.SystemKeyword == "AddNewBrand") == null)
            {
                repository.Insert(
                    new ActivityLogType {
                        SystemKeyword = "AddNewBrand",
                        Enabled = true,
                        Name = "Add a new brand"
                    });
            }
            if (repository.Table.FirstOrDefault(x => x.SystemKeyword == "EditBrand") == null)
            {
                repository.Insert(
                    new ActivityLogType {
                        SystemKeyword = "EditBrand",
                        Enabled = true,
                        Name = "Edit a brand"
                    });
            }
            if (repository.Table.FirstOrDefault(x => x.SystemKeyword == "DeleteBrand") == null)
            {
                repository.Insert(
                    new ActivityLogType {
                        SystemKeyword = "DeleteBrand",
                        Enabled = true,
                        Name = "Delete a brand"
                    });
            }
            if (repository.Table.FirstOrDefault(x => x.SystemKeyword == "EditShipment") == null)
            {
                repository.Insert(
                    new ActivityLogType {
                        SystemKeyword = "EditShipment",
                        Enabled = true,
                        Name = "Edit a shipment"
                    });
            }
            return true;
        }
    }
}
