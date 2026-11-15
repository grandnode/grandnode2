using Grand.Data;
using Grand.Domain.Admin;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._5;

/// <summary>
///     The "Merchandise returns" report in the menu, last under Reports, as the installer seeds it. Idempotent: an
///     existing entry is left alone, and a menu without the Reports section gets nothing
/// </summary>
public class MigrationMerchandiseReturnReportMenu : IMigration
{
    private const string NodeName = "Merchandise returns report";

    public int Priority => 3;
    public DbVersion Version => new(2, 5);
    public Guid Identity => new("8f4a5651-a47a-4954-a480-6228836de0ec");
    public string Name => "Merchandise return report in the admin menu 2.5";

    public async Task<bool> UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationMerchandiseReturnReportMenu>>();

        try
        {
            var reports = await repository.FirstOrDefaultAsync(repository.Table.Where(x => x.SystemName == "Reports"));
            if (reports == null || reports.ChildNodes.Any(x => x.SystemName == NodeName))
                return true;

            reports.ChildNodes.Add(new AdminSiteMap {
                SystemName = NodeName,
                ResourceName = "Admin.Reports.MerchandiseReturns",
                PermissionNames = new List<string> { PermissionSystemName.Reports },
                ControllerName = "Reports",
                ActionName = "MerchandiseReturnReport",
                DisplayOrder = reports.ChildNodes.Count == 0 ? 0 : reports.ChildNodes.Max(x => x.DisplayOrder) + 1,
                IconClass = "bi bi-record-circle"
            });
            await repository.UpdateAsync(reports);
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - MerchandiseReturnReportMenu (2.5)");
            return false;
        }

        return true;
    }
}
