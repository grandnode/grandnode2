using Grand.Data;
using Grand.Domain.Localization;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Module.Migration.Migrations;

public static class MigrationUpdateResource
{
    public static async Task<bool> ImportLanguageResourcesFromXml(this IServiceProvider serviceProvider,
        string filename)
    {
        var langRepository = serviceProvider.GetRequiredService<IRepository<Language>>();
        var lsrRepository = serviceProvider.GetRequiredService<IRepository<TranslationResource>>();
        var logService = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ImportLanguageResourcesFromXml");
        var hostingEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

        try
        {
            var language = await langRepository.FirstOrDefaultAsync(langRepository.Table.Where(l => l.Name == "English")) ??
                           await langRepository.FirstOrDefaultAsync(langRepository.Table);

            var filePath = Path.Combine(hostingEnvironment.ContentRootPath, filename);
            var localesXml = await File.ReadAllTextAsync(filePath);

            var xmlDoc = XmlExtensions.LanguageXmlDocument(localesXml);

            var translateResources = XmlExtensions.ParseTranslationResources(xmlDoc);

            foreach (var item in translateResources)
            {
                _ = Enum.TryParse(item.Area, out TranslationResourceArea areaEnum);

                await lsrRepository.InsertAsync(new TranslationResource {
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