using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Localization;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Extensions;

public static class LanguageServiceExtensions
{
    /// <summary>
    ///     Gets the language to write in when there is no request: the given language while it still exists,
    ///     otherwise the store's default language when the store offers it, otherwise the store's first published
    ///     language, otherwise any language
    /// </summary>
    /// <param name="languageService">Language service</param>
    /// <param name="languageId">Preferred language identifier; may be empty or point to a deleted language</param>
    /// <param name="store">Store whose languages are the fallback; null for all stores</param>
    /// <returns>The language, or null when no language exists at all</returns>
    public static async Task<Language> GetLanguageOrStoreDefault(this ILanguageService languageService,
        string languageId, Store store)
    {
        if (!string.IsNullOrEmpty(languageId))
        {
            var language = await languageService.GetLanguageById(languageId);
            if (language != null)
                return language;
        }

        var storeLanguages = await languageService.GetAllLanguages(storeId: store?.Id ?? "");
        return storeLanguages.FirstOrDefault(l => l.Id == store?.DefaultLanguageId)
               ?? storeLanguages.FirstOrDefault()
               ?? (await languageService.GetAllLanguages(true)).FirstOrDefault();
    }
}
