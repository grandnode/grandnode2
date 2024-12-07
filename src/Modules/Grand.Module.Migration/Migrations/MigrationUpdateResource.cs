using Grand.Data;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations;

public static class MigrationUpdateResource
{
    public static bool ImportLanguageResourcesFromXml(this IServiceProvider serviceProvider,
        string filename)
    {
        var langRepository = serviceProvider.GetRequiredService<IRepository<Language>>();
        var lsrRepository = serviceProvider.GetRequiredService<IRepository<TranslationResource>>();
        var logService = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ImportLanguageResourcesFromXml");
        var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        try
        {
            var language = langRepository.Table.FirstOrDefault(l => l.Name == "English") ?? langRepository.Table.FirstOrDefault();

            var filePath = Path.Combine(hostingEnvironment.ContentRootPath, filename);
            var localesXml = File.ReadAllText(filePath);

            var xmlDoc = XmlExtensions.LanguageXmlDocument(localesXml);

            var translateResources = XmlExtensions.ParseTranslationResources(xmlDoc);

            foreach (var item in translateResources)
            {
                _ = Enum.TryParse(item.Area, out TranslationResourceArea areaEnum);

                lsrRepository.Insert(new TranslationResource {
                    LanguageId = language!.Id,
                    Name = item.Name.ToLowerInvariant(),
                    Value = item.Value,
                    Area = areaEnum,
                    CreatedBy = "System"
                });
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - ImportLanguageResourcesFromXml {Filename}", filename);
        }

        return true;
    }
}