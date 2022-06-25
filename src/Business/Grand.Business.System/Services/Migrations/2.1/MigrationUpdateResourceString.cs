using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;

namespace Grand.Business.System.Services.Migrations._2._1
{
    public class MigrationUpdateResourceString : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(2, 1);
        public Guid Identity => new("A095104A-b784-4DA7-8380-252A0C3C7404");
        public string Name => "Update resource string for english language 2.1";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_201.xml");
        }
    }
}