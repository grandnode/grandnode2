using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationUpdateResourceString : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(1, 1);
        public Guid Identity => new("5FE5E3D4-2783-4925-8727-A8D8F202E4B8");
        public string Name => "Update resource string for english language";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_110.xml");
        }
    }
}
