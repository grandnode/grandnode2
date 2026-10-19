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
            if (language == null)
            {
                //no language to import into - nothing to do
                logService.LogWarning("UpgradeProcess - ImportLanguageResourcesFromXml {Filename}: no language found", filename);
                return true;
            }

            var filePath = Path.Combine(hostingEnvironment.ContentRootPath, filename);
            var localesXml = await File.ReadAllTextAsync(filePath);

            var xmlDoc = XmlExtensions.LanguageXmlDocument(localesXml);

            var translateResources = XmlExtensions.ParseTranslationResources(xmlDoc);

            //a failed run is retried on the next start - skip the resources it already inserted
            var existingNames = (await lsrRepository.ToListAsync(lsrRepository.Table
                    .Where(x => x.LanguageId == language.Id)
                    .Select(x => x.Name)))
                .ToHashSet();

            foreach (var item in translateResources)
            {
                var name = item.Name.ToLowerInvariant();
                if (!existingNames.Add(name)) continue;

                _ = Enum.TryParse(item.Area, out TranslationResourceArea areaEnum);

                await lsrRepository.InsertAsync(new TranslationResource {
                    LanguageId = language.Id,
                    Name = name,
                    Value = item.Value,
                    Area = areaEnum,
                    CreatedBy = "System"
                });
            }
        }
        catch (Exception ex)
        {
            logService.LogError(ex, "UpgradeProcess - ImportLanguageResourcesFromXml {Filename}", filename);
            return false;
        }

        return true;
    }
}