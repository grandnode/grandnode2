using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

/// <summary>
///     Removes the Help node from the admin menu, with everything under it ("Premium support services").
///     The installer no longer seeds it. Idempotent.
/// </summary>
public class MigrationRemoveAdminSiteMapHelp : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("509D9237-9959-46CF-990A-7D04BFB41434");
    public string Name => "Remove the Help node from the admin site map 2.4";

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
            if (help != null)
                repository.Delete(help);
            return true;
        }
        catch (Exception ex)
        {
            //false stops the migration process here and retries it on the next start; true would
            //record the migration as applied and never run it again
            logService.LogError(ex, "UpgradeProcess - RemoveAdminSiteMapHelp (2.4)");
            return false;
        }
    }
}
