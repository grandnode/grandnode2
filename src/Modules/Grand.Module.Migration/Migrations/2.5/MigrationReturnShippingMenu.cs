using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     The "Return methods" entry of the admin menu, under Shipping right after the shipping providers, as the installer
///     seeds it. Idempotent: an existing entry is left alone, and a menu without the Shipping section gets nothing
/// </summary>
public class MigrationReturnShippingMenu : IMigration
{
    private const string NodeName = "Return methods";

    public int Priority => 3;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("eb33b695-1fea-454e-a61e-db49260d8f33");
    public string Name => "Merchandise return: return methods in the admin menu 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationReturnShippingMenu>>();

        try
        {
            var configuration =
                await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "Configuration"));
            var shipping = configuration?.ChildNodes.FirstOrDefault(x => x.SystemName == "Shipping");
            if (shipping == null || shipping.ChildNodes.Any(x => x.SystemName == NodeName))
                return true;

            //the display order of the shipping providers; the menu's ordering is stable, so it follows them
            shipping.ChildNodes.Add(new AdminSiteMap {
                SystemName = NodeName,
                ResourceName = "Admin.Configuration.Shipping.ReturnMethods",
                ControllerName = "ReturnShipping",
                ActionName = "Providers",
                DisplayOrder = 0,
                IconClass = "bi bi-record-circle"
            });
            await repository.UpdateAsync(configuration);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - ReturnShippingMenu (2.5)");
            return false;
        }

        return true;
    }
}
