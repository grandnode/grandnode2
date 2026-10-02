using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Removes the "Premium support services" link from the admin menu, and the Help node that held it
///     once nothing is left under it. The installer no longer seeds either.
///     Idempotent; a Help node the operator added their own entries to keeps them and stays.
/// </summary>
public class MigrationRemoveAdminSiteMapHelp : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("509D9237-9959-46CF-990A-7D04BFB41434");
    public string Name => "Remove Premium support services and the empty Help node from the admin site map 2.4";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationRemoveAdminSiteMapHelp>>();

        try
        {
            var help = repository.Table.FirstOrDefault(x => x.SystemName == "Help");
            if (help == null) return true;

            help.ChildNodes = help.ChildNodes.Where(x => x.SystemName != "Premium support services").ToList();

            if (help.ChildNodes.Any())
                repository.Update(help);
            else
                repository.Delete(help);
        }
        catch (InvalidOperationException ex)
        {
            logService.LogError(ex, "UpgradeProcess - RemoveAdminSiteMapHelp (2.4)");
        }

        return true;
    }
}
