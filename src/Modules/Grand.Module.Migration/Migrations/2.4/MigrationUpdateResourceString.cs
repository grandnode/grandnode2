using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._4;

public class MigrationUpdateResourceString : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("6C0F2E4B-9A17-4D65-B3C8-51E0A9D77B42");
    public string Name => "Update resource string for english language 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_240.xml");
    }
}
