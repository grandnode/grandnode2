using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._1
{
    public class MigrationUpdateCustomerSecuritySettings : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(2, 1);
        public Guid Identity => new("4B972F99-CDEB-4521-919F-50C2376CA6FA");
        public string Name => "Sets default values for new Customer Security config settings";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<ISettingService>();
            var logService = serviceProvider.GetRequiredService<ILogger>();

            try
            {

                repository.SaveSetting(new CustomerSettings {
                    LoginWithMagicLinkEnabled = false,
                    LoginCodeMinutesToExpire = 10
                });
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - Add new Customer Security Settings", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }
    }
}