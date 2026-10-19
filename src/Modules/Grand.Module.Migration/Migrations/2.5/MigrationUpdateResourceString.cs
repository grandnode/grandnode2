using Grand.Infrastructure.Migrations;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationUpdateResourceString : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("15EDC88E-A865-4BA2-9E43-FB97839F1B92");
    public string Name => "Update resource string for english language 2.5";

    public Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        return serviceProvider.ImportLanguageResourcesFromXml("App_Data/Resources/Upgrade/en_250.xml");
    }
}