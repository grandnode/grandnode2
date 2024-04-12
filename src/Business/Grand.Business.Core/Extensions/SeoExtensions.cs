using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.SharedKernel.Extensions;

namespace Grand.Business.Core.Extensions;

public static class SeoExtensions
{
    #region Product tag

    /// <summary>
    ///     Gets product tag SE (search engine) name
    /// </summary>
    /// <param name="productTag">Product tag</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>Product tag SE (search engine) name</returns>
    public static string GetSeName(this ProductTag productTag, string languageId)
    {
        ArgumentNullException.ThrowIfNull(productTag);
        var seName = GenerateSlug(productTag.GetTranslation(x => x.Name, languageId), false, false, false);
        return seName;
    }

    #endregion

    #region Fields

    private static Dictionary<string, string> _seoCharacterTable;
    private static readonly object SLock = new();

    #endregion

    #region General

    /// <summary>
    ///     Get search engine friendly name (slug)
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="entity">Entity</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="returnDefaultValue">
    ///     A value indicating whether to return default value (if language specified one is not
    ///     found)
    /// </param>
    /// <returns>Search engine  name (slug)</returns>
    public static string GetSeName<T>(this T entity, string languageId, bool returnDefaultValue = true)
        where T : BaseEntity, ISlugEntity, ITranslationEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var seName = string.Empty;
        if (!string.IsNullOrEmpty(languageId))
        {
            var value = entity.Locales.FirstOrDefault(x => x.LanguageId == languageId && x.LocaleKey == "SeName");
            if (value != null)
                if (!string.IsNullOrEmpty(value.LocaleValue))
                    seName = value.LocaleValue;
        }

        //set default value if required
        if (string.IsNullOrEmpty(seName) && returnDefaultValue) seName = entity.SeName;

        return seName;
    }

    /// <summary>
    ///     Validate search engine name
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <param name="seName">Search engine name to validate</param>
    /// <param name="name">User-friendly name used to generate se-name</param>
    /// <param name="ensureNotEmpty">Ensure that se-name is not empty</param>
    /// <param name="seoSettings"></param>
    /// <param name="slugService"></param>
    /// <param name="languageService"></param>
    /// <returns>Valid se-name</returns>
    public static async Task<string> ValidateSeName<T>(this T entity, string seName, string name, bool ensureNotEmpty,
        SeoSettings seoSettings, ISlugService slugService, ILanguageService languageService)
        where T : BaseEntity, ISlugEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        //use name if se-name is not specified
        if (string.IsNullOrWhiteSpace(seName) && !string.IsNullOrWhiteSpace(name))
            seName = name;

        //validation
        seName = GetSeName(seName, seoSettings);

        //max length
        //For long URLs we can get the following error:
        //"the specified path, file name, or both are too long. The fully qualified file name must be less than 260 characters, and the directory name must be less than 248 characters"
        //that's why we limit it to 200 here (consider a store URL + probably added {0}-{1} below)
        seName = CommonHelper.EnsureMaximumLength(seName, 200);

        if (string.IsNullOrWhiteSpace(seName))
        {
            if (ensureNotEmpty)
                //use entity identifier as se-name if empty
                seName = entity.Id;
            else
                //return. no need for further processing
                return seName;
        }

        //ensure this se-name is not reserved yet
        var entityName = typeof(T).Name;
        var i = 1;
        var tempSeName = seName;
        while (true)
        {
            //check whether such slug already exists (and that is not the current entity)
            var entityUrl = await slugService.GetBySlug(tempSeName);
            var reserved1 = entityUrl != null && !(entityUrl.EntityId == entity.Id &&
                                                   entityUrl.EntityName.Equals(entityName,
                                                       StringComparison.OrdinalIgnoreCase));
            //and it's not in the list of reserved slugs
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


    /// <summary>
    ///     Get SE name
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="seoSettings">SeoSettings</param>
    /// <returns>Result</returns>
    public static string GetSeName(string name, SeoSettings seoSettings)
    {
        return GenerateSlug(name, seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls,
            seoSettings.AllowSlashChar, seoSettings.SeoCharConversion);
    }

    /// <summary>
    ///     Get SE name
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="convertNonWesternChars"></param>
    /// <param name="allowUnicodeCharsInUrls"></param>
    /// <param name="charConversions"></param>
    /// <returns>Result</returns>
    public static string GetSeName(string name, bool convertNonWesternChars, bool allowUnicodeCharsInUrls,
        string charConversions = null)
    {
        return GenerateSlug(name, convertNonWesternChars, allowUnicodeCharsInUrls, false, charConversions);
    }

    /// <summary>
    ///     Get SE name
    /// </summary>
    /// <param name="name">Name</param>
    /// <param name="convertNonWesternChars">A value indicating whether non western chars should be converted</param>
    /// <param name="allowUnicodeCharsInUrls">A value indicating whether Unicode chars are allowed</param>
    /// <param name="allowSlashChar"></param>
    /// <param name="charConversions"></param>
    /// <returns>Result</returns>
    public static string GenerateSlug(string name, bool convertNonWesternChars, bool allowUnicodeCharsInUrls,
        bool allowSlashChar, string charConversions = null)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var okChars = "abcdefghijklmnopqrstuvwxyz1234567890 _-";
        if (allowSlashChar)
            okChars += '/';

        name = name.Trim().ToLowerInvariant();

        if (convertNonWesternChars)
            if (!string.IsNullOrEmpty(charConversions) && _seoCharacterTable == null)
                InitializeSeoCharacterTable(charConversions);

        var sb = new StringBuilder();
        foreach (var c in name.ToCharArray())
        {
            var c2 = c.ToString();
            if (convertNonWesternChars && _seoCharacterTable != null)
                if (_seoCharacterTable.ContainsKey(c2))
                    c2 = _seoCharacterTable[c2];

            if (allowUnicodeCharsInUrls)
            {
                if (char.IsLetterOrDigit(c) || okChars.Contains(c2))
                    sb.Append(c2);
            }
            else if (okChars.Contains(c2))
            {
                sb.Append(c2);
            }
        }

        var name2 = sb.ToString();
        name2 = name2.Replace(" ", "-");
        while (name2.Contains("--"))
            name2 = name2.Replace("--", "-");
        while (name2.Contains("__"))
            name2 = name2.Replace("__", "_");
        return name2;
    }

    private static void InitializeSeoCharacterTable(string charConversions)
    {
        lock (SLock)
        {
            if (_seoCharacterTable != null) return;
            _seoCharacterTable = new Dictionary<string, string>();

            foreach (var conversion in charConversions.Split(";"))
            {
                var strLeft = conversion.Split(":").FirstOrDefault();
                var strRight = conversion.Split(":").LastOrDefault();
                if (!string.IsNullOrEmpty(strLeft) && !_seoCharacterTable.ContainsKey(strLeft))
                    _seoCharacterTable.Add(strLeft.Trim(), strRight?.Trim());
            }
        }
    }

    #endregion
}