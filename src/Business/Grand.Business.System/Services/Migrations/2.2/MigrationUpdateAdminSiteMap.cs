using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Admin;
using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._2
{
    public class MigrationUpdateAdminSiteMap : IMigration
    {

        public int Priority => 0;
        public DbVersion Version => new(2, 2);
        public Guid Identity => new("1025D405-FFF3-45E1-A240-2E5F2A92535F");
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
            var logService = serviceProvider.GetRequiredService<ILogger>();

            try
            {
                var sitemapSystem = repository.Table.FirstOrDefault(x => x.SystemName == "Configuration");
                if (sitemapSystem != null)
                {
                    sitemapSystem.ChildNodes.Add(new AdminSiteMap() {
                        SystemName = "Admin menu",
                        ResourceName = "Admin.Configuration.Menu",
                        PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                        ControllerName = "Menu",
                        ActionName = "Index",
                        DisplayOrder = 7,
                        IconClass = "fa fa-dot-circle-o"
                    });
                    repository.Update(sitemapSystem);
                }
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - UpdateAdminSiteMap", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }
    }
}
