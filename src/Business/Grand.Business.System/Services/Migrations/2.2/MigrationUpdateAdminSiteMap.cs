using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Admin;
using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations._2._2
{
    public class MigrationUpdateAdminSiteMap : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(2, 2);
        public Guid Identity => new("55E49962-66DD-4034-8AB2-7F4C33A5E0B2");
        public string Name => "Update standard admin site map - add admin menu";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var repository = serviceProvider.GetRequiredService<IRepository<Domain.Admin.AdminSiteMap>>();
            var logService = serviceProvider.GetRequiredService<ILogger<MigrationUpdateAdminSiteMap>>();

            try
            {
                var sitemapConfiguration = repository.Table.FirstOrDefault(x => x.SystemName == "Configuration");
                if (sitemapConfiguration != null)
                {
                    sitemapConfiguration.ChildNodes.Add(new AdminSiteMap() {
                        SystemName = "Admin menu",
                        ResourceName = "Admin.Configuration.Menu",
                        PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                        ControllerName = "Menu",
                        ActionName = "Index",
                        DisplayOrder = 7,
                        IconClass = "fa fa-dot-circle-o"
                    });
                    repository.Update(sitemapConfiguration);
                }

                var sitemapSystem = repository.Table.FirstOrDefault(x => x.SystemName == "System");
                if (sitemapSystem != null)
                {
                    var log = sitemapSystem.ChildNodes.FirstOrDefault(x => x.SystemName == "Log");
                    sitemapSystem.ChildNodes.Remove(log);
                    sitemapSystem.PermissionNames.Remove("ManageSystemLog");
                    repository.Update(sitemapSystem);
                }
            }
            catch (Exception ex)
            {
                logService.LogError(ex, "UpgradeProcess - UpdateAdminSiteMap");
            }

            return true;
        }
    }
}