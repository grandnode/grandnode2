using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Domain.Data;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Migrations
{
    public static class MigrationUpdateResource
    {
        public static bool ImportLanguageResourcesFromXml(this IServiceProvider serviceProvider,
            string filename)
        {
            var langRepository = serviceProvider.GetRequiredService<IRepository<Language>>();
            var logService = serviceProvider.GetRequiredService<ILogger>();
            var translationService = serviceProvider.GetRequiredService<ITranslationService>();

            try
            {
                var language = langRepository.Table.FirstOrDefault(l => l.Name == "English");

                if (language == null)
                    language = langRepository.Table.FirstOrDefault();

                var filePath = CommonPath.MapPath(filename);
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
