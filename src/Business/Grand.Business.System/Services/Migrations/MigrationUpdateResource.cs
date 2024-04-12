using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Data;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Business.System.Services.Migrations;

public static class MigrationUpdateResource
{
    public static bool ImportLanguageResourcesFromXml(this IServiceProvider serviceProvider,
        string filename)
    {
        var langRepository = serviceProvider.GetRequiredService<IRepository<Language>>();
        var logService = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("ImportLanguageResourcesFromXml");
        var translationService = serviceProvider.GetRequiredService<ITranslationService>();

        try
        {
            var language = langRepository.Table.FirstOrDefault(l => l.Name == "English") ??
                           langRepository.Table.FirstOrDefault();

            var filePath = CommonPath.MapPath(filename);
            var localesXml = File.ReadAllText(filePath);
            translationService.ImportResourcesFromXmlInstall(language, localesXml).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - ImportLanguageResourcesFromXml {Filename}", filename);
        }

        return true;
    }
}