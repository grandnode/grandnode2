using Grand.Data;
using Grand.Domain.Admin;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations._2._4;

public class MigrationUpdateAdminSiteMap : IMigration
{
    public int Priority => 1;
    public DbVersion Version => new(2, 4);
    public Guid Identity => new("B7C3D8E2-F1A4-4B9E-8C62-197E3F5A4D21");
    public string Name => "Update standard admin site map - restrict Vendor, Push notifications, Admin search, System settings to System permission";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateAdminSiteMap>>();

        try
        {
            var sitemapSettings = repository.Table.FirstOrDefault(x => x.SystemName == "Settings");
            if (sitemapSettings == null) return true;

            var adminOnlyItems = new[] {
                "Vendor settings",
                "Push notifications settings",
                "Admin search settings",
                "System settings"
            };

            foreach (var itemName in adminOnlyItems)
            {
                var item = sitemapSettings.ChildNodes.FirstOrDefault(x => x.SystemName == itemName);
                if (item != null && !item.PermissionNames.Contains(PermissionSystemName.System))
                    item.PermissionNames.Add(PermissionSystemName.System);
            }

            repository.Update(sitemapSettings);
        }
        catch (InvalidOperationException ex)
        {
            logService.LogError(ex, "UpgradeProcess - UpdateAdminSiteMap (2.4)");
        }

        return true;
    }
}
