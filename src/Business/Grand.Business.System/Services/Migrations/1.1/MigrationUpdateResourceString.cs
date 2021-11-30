using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.Infrastructure.Migrations;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;

namespace Grand.Business.System.Services.Migrations._1._1
{
    public class MigrationUpdateResourceString : IMigration
    {
        public int Priority => 0;
        public DbVersion Version => new(1, 1);
        public Guid Identity => new("5FE5E3D4-2783-4925-8727-A8D8F202E4B8");
        public string Name => "Update resource string for english language";

        /// <summary>
        /// Upgrade process
        /// </summary>
        /// <param name="database"></param>
        /// <param name="serviceProvider"></param>
        /// <returns></returns>
        public bool UpgradeProcess(IDatabaseContext database, IServiceProvider serviceProvider)
        {
            var langRepository = serviceProvider.GetRequiredService<IRepository<Language>>();
            var logService = serviceProvider.GetRequiredService<ILogger>();
            var translationService = serviceProvider.GetRequiredService<ITranslationService>();

            try
            {
                var language = langRepository.Table.FirstOrDefault(l => l.Name == "English");

                if (language == null)
                    language = langRepository.Table.FirstOrDefault();

                var filePath = CommonPath.MapPath("App_Data/Resources/Upgrade/en_110.xml");
                var localesXml = File.ReadAllText(filePath);
                translationService.ImportResourcesFromXmlInstall(language, localesXml).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logService.InsertLog(Domain.Logging.LogLevel.Error, "UpgradeProcess - MigrationUpdateResourceString", ex.Message).GetAwaiter().GetResult();
            }
            return true;
        }
    }
}
