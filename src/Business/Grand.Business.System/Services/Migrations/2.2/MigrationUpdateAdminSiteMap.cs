using Grand.Business.Core.Utilities.Common.Security;
using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._2;

public class MigrationUpdateAdminSiteMap : IMigration
{
    public int Priority => 0;
    public DbVersion Version => new(2, 2);
    public Guid Identity => new("D548BACF-3DF8-48ED-A522-086EAB165808");
    public string Name => "Update standard admin site map - add admin menu";

    /// <summary>
    ///     Upgrade process
    /// </summary>
    /// <param name="database"></param>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
    {
        var repository = serviceProvider.GetRequiredService<IRepository<AdminSiteMap>>();
        var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateAdminSiteMap>>();

        try
        {
            var sitemapConfiguration = repository.Table.FirstOrDefault(x => x.SystemName == "Configuration");
            if (sitemapConfiguration != null)
            {
                if (sitemapConfiguration.ChildNodes.FirstOrDefault(x => x.SystemName == "Admin menu") == null)
                    sitemapConfiguration.ChildNodes.Add(new AdminSiteMap {
                        SystemName = "Admin menu",
                        ResourceName = "Admin.Configuration.Menu",
                        PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                        ControllerName = "Menu",
                        ActionName = "Index",
                        DisplayOrder = 7,
                        IconClass = "fa fa-dot-circle-o"
                    });
                sitemapConfiguration.PermissionNames.Remove("ManageActivityLog");
                repository.Update(sitemapConfiguration);
            }

            var sitemapSystem = repository.Table.FirstOrDefault(x => x.SystemName == "System");
            var log = sitemapSystem?.ChildNodes.FirstOrDefault(x => x.SystemName == "Log");
            if (log != null)
            {
                sitemapSystem.ChildNodes.Remove(log);
                sitemapSystem.PermissionNames.Remove("ManageSystemLog");
                repository.Update(sitemapSystem);
            }

            var sitemapCustomers = repository.Table.FirstOrDefault(x => x.SystemName == "Customers");
            var activityLog = sitemapCustomers?.ChildNodes.FirstOrDefault(x => x.SystemName == "Activity Log");
            if (activityLog != null)
            {
                sitemapCustomers.ChildNodes.Remove(activityLog);
                sitemapCustomers.PermissionNames.Remove("ManageActivityLog");
                repository.Update(sitemapCustomers);
            }

            var sitemapReports = repository.Table.FirstOrDefault(x => x.SystemName == "Reports");
            var activityStatLog = sitemapReports?.ChildNodes.FirstOrDefault(x => x.SystemName == "Activity Stats");
            if (activityStatLog != null)
            {
                sitemapReports.ChildNodes.Remove(activityStatLog);
                sitemapReports.PermissionNames.Remove("ManageActivityLog");
                repository.Update(sitemapReports);
            }

            var sitemapSettings = repository.Table.FirstOrDefault(x => x.SystemName == "Settings");
            var activityTypes = sitemapSettings?.ChildNodes.FirstOrDefault(x => x.SystemName == "Activity Types");
            if (activityTypes != null)
            {
                sitemapSettings.ChildNodes.Remove(activityTypes);
                sitemapSettings.PermissionNames.Remove("ManageActivityLog");
                repository.Update(sitemapSettings);
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - UpdateAdminSiteMap");
        }

        return true;
    }
}