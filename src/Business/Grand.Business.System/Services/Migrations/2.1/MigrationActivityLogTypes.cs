using Grand.Domain.Data;
using Grand.Domain.Logging;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._1
{
    public class MigrationActivityLogTypes : IMigration
    {

        public int Priority => 0;
        public DbVersion Version => new(2, 1);
        public Guid Identity => new("6B364BD3-CB5D-4060-B5ED-E3D8E0D1DF6B");
        public string Name => "Remove activity log types";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IRepository<ActivityLogType>>();
            var logService = serviceProvider.GetRequiredService<ILogger<MigrationActivityLogTypes>>();
            try
            {
                var activityLogTypes = repository
                    .Table.Where(x =>
                        x.SystemKeyword == "InteractiveFormDelete" ||
                        x.SystemKeyword == "InteractiveFormEdit" ||
                        x.SystemKeyword == "InteractiveFormAdd" ||
                        x.SystemKeyword == "PublicStore.InteractiveForm" ||
                        x.SystemKeyword == "CustomerReminder.AbandonedCart" ||
                        x.SystemKeyword == "CustomerReminder.RegisteredCustomer" ||
                        x.SystemKeyword == "CustomerReminder.LastActivity" ||
                        x.SystemKeyword == "CustomerReminder.LastPurchase" ||
                        x.SystemKeyword == "CustomerReminder.Birthday" ||
                        x.SystemKeyword == "CustomerReminder.SendCampaign");
                foreach (var item in activityLogTypes)
                {
                    repository.Delete(item);
                }

            }
            catch (Exception ex)
            {
                logService.LogError(ex, "UpgradeProcess - RemoveOldActivityLogType");
            }
            return true;

        }
    }
}
