using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

public class MigrationAddDesignSiteMap : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("b9183ae7-1eeb-468c-a3c1-c3013e241ded");
    public string Name => "Add Design > Branding to admin site map";

    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationAddDesignSiteMap>>();

        try
        {
            if (repository.Table.Any(x => x.SystemName == "Design"))
                return true;

            var designMenu = new AdminSiteMap {
                SystemName = "Design",
                ResourceName = "Admin.Design",
                IconClass = "fa fa-paint-brush",
                DisplayOrder = 6,
                ChildNodes = new List<AdminSiteMap> {
                    new() {
                        SystemName = "Branding",
                        ResourceName = "Admin.Design.Branding",
                        ControllerName = "Branding",
                        ActionName = "Index",
                        DisplayOrder = 0,
                        IconClass = "fa fa-dot-circle-o"
                    }
                }
            };

            repository.Insert(designMenu);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - AddDesignSiteMap");
        }

        return true;
    }
}
