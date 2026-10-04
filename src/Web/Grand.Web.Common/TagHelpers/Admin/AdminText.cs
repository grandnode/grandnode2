using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;

namespace Grand.Web.Common.TagHelpers.Admin;

/// <summary>
///     The chrome texts of the admin components. All three panels read the same
///     <c>Admin.Common.*</c> keys, as the grid does.
/// </summary>
internal static class AdminText
{
    /// <summary>
    ///     The resource, or <paramref name="fallback" /> when the database does not carry the key -
    ///     a 2.4 development database that never imported en_240.xml, for instance. A component
    ///     shows readable English rather than a raw resource key.
    /// </summary>
    public static string Resource(ITranslationService translationService, IContextAccessor contextAccessor,
        string key, string fallback)
    {
        var languageId = contextAccessor?.WorkContext?.WorkingLanguage?.Id;
        if (languageId is null) return fallback;
        var value = translationService.GetResource(key, languageId, string.Empty, true);
        return string.IsNullOrEmpty(value) ? fallback : value;
    }
}
