using Grand.Data;
using Grand.Infrastructure.Migrations;

namespace Grand.Business.System.Services.Migrations._2._1;

public class MigrationUpdateResourceString : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 1);
    public Guid Identity => new("2803EE48-7875-4AB9-BDFA-3B1FBC7CC37E");
    public string Name => "Update resource string for english language 2.1";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_210.xml");
    }
}