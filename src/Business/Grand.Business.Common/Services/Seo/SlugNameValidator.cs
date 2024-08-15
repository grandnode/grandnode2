using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain;
using Grand.Domain.Seo;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Common.Services.Seo;

public class SeNameService(ISlugService slugService, ILanguageService languageService, SeoSettings seoSettings) : ISeNameService
{
    public async Task<string> ValidateSeName<T>(T entity, string seName, string name, bool ensureNotEmpty) where T : BaseEntity, ISlugEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (string.IsNullOrWhiteSpace(seName) && !string.IsNullOrWhiteSpace(name))
            seName = name;

        seName = GetSeName(seName);

        seName = CommonHelper.EnsureMaximumLength(seName, 200);

        if (string.IsNullOrWhiteSpace(seName))
        {
            if (ensureNotEmpty)
                seName = entity.Id;
            else
                return seName;
        }

        var entityName = typeof(T).Name;
        var i = 1;
        var tempSeName = seName;
        
        while (true)
        {
            var entityUrl = await slugService.GetBySlug(tempSeName);
            var reserved1 = entityUrl != null && !(entityUrl.EntityId == entity.Id &&
                                                   entityUrl.EntityName.Equals(entityName,
                                                       StringComparison.OrdinalIgnoreCase));
            
            var reserved2 = seoSettings.ReservedEntityUrlSlugs.Contains(tempSeName, StringComparer.OrdinalIgnoreCase);
            var reserved3 = (await languageService.GetAllLanguages(true)).Any(language =>
                language.UniqueSeoCode.Equals(tempSeName, StringComparison.OrdinalIgnoreCase));
            
            if (!reserved1 && !reserved2 && !reserved3)
                break;

            tempSeName = $"{seName}-{i}";
            i++;
            if (i <= 4) continue;
            tempSeName = $"{seName}-{Guid.NewGuid()}";
            break;
        }

        seName = tempSeName;

        return seName;
    }

    private string GetSeName(string name)
    {
        return SeoExtensions.GenerateSlug(name, seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls,
            seoSettings.AllowSlashChar, seoSettings.SeoCharConversion);
    }
}