using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationUpdateAdminSiteMap : IMigration
    {

        public int Priority => 0;

        public DbVersion Version => new(1, 1);

        public Guid Identity => new("7D8642FC-2FAA-4AC1-B062-5E3513DAC658");

        public string Name => "Update standard admin site map";

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
                var sitemap = repository.Table.FirstOrDefault(x => x.SystemName == "System");
                if (sitemap != null)
                {
                    sitemap.PermissionNames = new List<string> { PermissionSystemName.SystemLog, PermissionSystemName.MessageQueue, PermissionSystemName.MessageContactForm,
                        PermissionSystemName.Maintenance, PermissionSystemName.ScheduleTasks, PermissionSystemName.System };
                    var childnodeDevTools = sitemap.ChildNodes.FirstOrDefault(x => x.SystemName == "Developer tools");
                    if (childnodeDevTools != null)
                    {
                        childnodeDevTools.PermissionNames = new List<string> { PermissionSystemName.Maintenance, PermissionSystemName.System };
                        var childnodeRoslyn = childnodeDevTools.ChildNodes.FirstOrDefault(x => x.SystemName == "Roslyn compiler");
                        if (childnodeRoslyn != null)
                        {
                            childnodeRoslyn.PermissionNames = new List<string> { PermissionSystemName.System };
                            childnodeRoslyn.ControllerName = "System";
                        }
                    }
                    var childnodeSysInformation = sitemap.ChildNodes.FirstOrDefault(x => x.SystemName == "System information");
                    if (childnodeSysInformation != null)
                        childnodeSysInformation.ControllerName = "System";

                    repository.Update(sitemap);
                }
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - UpdateAdminSiteMap", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }

        private class SettingsMedia
        {
            public bool StoreInDb { get; set; }
        }

    }
}
