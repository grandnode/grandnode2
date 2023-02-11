﻿using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Data;
using Grand.Infrastructure.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations._2._1
{
    public class MigrationUpdateAdminSiteMap : IMigration
    {

        public int Priority => 0;
        public DbVersion Version => new(2, 1);
        public Guid Identity => new("8E1C04EB-4A76-4C6F-91E2-4C873EDA9C14");
        public string Name => "Update standard admin site map - remove unused";

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
                var sitemapSystem = repository.Table.FirstOrDefault(x => x.SystemName == "Marketing");
                if (sitemapSystem != null)
                {
                    sitemapSystem.PermissionNames = new List<string> { PermissionSystemName.Affiliates, PermissionSystemName.NewsletterCategories, PermissionSystemName.NewsletterSubscribers,
                        PermissionSystemName.Campaigns, PermissionSystemName.Discounts, PermissionSystemName.PushNotifications,
                        PermissionSystemName.Affiliates, PermissionSystemName.Documents, PermissionSystemName.GiftVouchers,
                        PermissionSystemName.ContactAttributes};

                    sitemapSystem.ChildNodes.Remove(sitemapSystem.ChildNodes.FirstOrDefault(x => x.SystemName == "Customer reminders"));
                    sitemapSystem.ChildNodes.Remove(sitemapSystem.ChildNodes.FirstOrDefault(x => x.SystemName == "Customer actions"));

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
