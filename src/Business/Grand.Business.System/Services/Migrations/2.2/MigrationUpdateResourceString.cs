using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;

namespace Grand.Business.System.Services.Migrations._2._2
{
    public class MigrationUpdateResourceString : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(2, 2);
        public Guid Identity => new("40AE285E-247E-4ABA-A892-479F22297580");
        public string Name => "Update resource string for english language 2.2";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_220.xml");
        }
    }
}
