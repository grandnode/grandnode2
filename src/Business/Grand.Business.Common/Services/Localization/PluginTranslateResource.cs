using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;

namespace Grand.Business.Common.Services.Localization;

public class PluginTranslateResource(ITranslationService translationService, ILanguageService languageService) : IPluginTranslateResource
{
    /// <summary>
    ///     Add a translate resource (if new) or update an existing one
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="value">Value</param>
    /// <param name="area">Area</param>
    /// <param name="languageCulture">Language culture code. If null or empty, then a resource will be added for all languages</param>
    public virtual async Task AddOrUpdatePluginTranslateResource(string name, string value, TranslationResourceArea area = TranslationResourceArea.Common, string languageCulture = null)
    {
        if (!string.IsNullOrEmpty(name))
            name = name.ToLowerInvariant();
        foreach (var lang in await languageService.GetAllLanguages(true))
        {
            if (!string.IsNullOrEmpty(languageCulture) && !languageCulture.Equals(lang.LanguageCulture))
                continue;

            var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
            if (lsr == null)
            {
                lsr = new TranslationResource {
                    LanguageId = lang.Id,
                    Name = name,
                    Value = value,
                    Area = area
                };
                await translationService.InsertTranslateResource(lsr);
            }
            else
            {
                lsr.Value = value;
                lsr.Area = area;
                await translationService.UpdateTranslateResource(lsr);
            }
        }
    }

    /// <summary>
    ///     Delete a translation resource
    /// </summary>
    /// <param name="name">Name</param>
    public virtual async Task DeletePluginTranslationResource(string name)
    {
        if (!string.IsNullOrEmpty(name))
            name = name.ToLowerInvariant();
        
        foreach (var lang in await languageService.GetAllLanguages(true))
        {
            var resource = await translationService.GetTranslateResourceByName(name, lang.Id);
            if (resource != null)
                await translationService.DeleteTranslateResource(resource);
        }
    }
}