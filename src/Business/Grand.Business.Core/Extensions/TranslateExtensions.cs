using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using System.Linq.Expressions;
using System.Reflection;

namespace Grand.Business.Core.Extensions;

public static class TranslateExtensions
{
    /// <summary>
    ///     Get translation property of an entity
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    /// <param name="entity">Entity</param>
    /// <param name="keySelector">Key selector</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="returnDefaultValue">A value indicating whether to return default value (if translation is not found)</param>
    /// <returns>Translation property</returns>
    public static string GetTranslation<T>(this T entity,
        Expression<Func<T, string>> keySelector, string languageId,
        bool returnDefaultValue = true)
        where T : ParentEntity, ITranslationEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (keySelector.Body is not MemberExpression member)
            throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");

        var propInfo = member.Member as PropertyInfo;
        if (propInfo == null)
            throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");

        string result = default;
        var resultStr = string.Empty;

        var localeKey = propInfo.Name;

        if (!string.IsNullOrEmpty(languageId))
            if (entity.Locales.Any())
            {
                var en = entity.Locales.FirstOrDefault(x => x.LanguageId == languageId && x.LocaleKey == localeKey);
                if (en != null)
                {
                    resultStr = en.LocaleValue;
                    if (!string.IsNullOrEmpty(resultStr))
                        result = resultStr;
                }
            }

        //set default value if required
        if (string.IsNullOrEmpty(resultStr) && returnDefaultValue) result = (string)propInfo.GetValue(entity);
        return result;
    }

    /// <summary>
    ///     Get translation value of enum
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="enumValue">Enum value</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="workContext">Work context</param>
    /// <returns>Translation value</returns>
    public static string GetTranslationEnum<T>(this T enumValue,
        ITranslationService translationService,
        IWorkContext workContext)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(workContext);

        return GetTranslationEnum(enumValue, translationService, workContext.WorkingLanguage.Id);
    }

    /// <summary>
    ///     Get translation value of enum
    /// </summary>
    /// <typeparam name="T">Enum</typeparam>
    /// <param name="enumValue">Enum value</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>Translation value</returns>
    public static string GetTranslationEnum<T>(this T enumValue,
        ITranslationService translationService, string languageId)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(translationService);

        if (!typeof(T).GetTypeInfo().IsEnum) throw new ArgumentException("T must be enum type");

        //Translation value
        var resourceName = $"Enums.{typeof(T)}.{enumValue.ToString()}";
        var result = translationService.GetResource(resourceName, languageId, "", true);

        //set default value if required
        if (string.IsNullOrEmpty(result))
            result = CommonHelper.ConvertEnum(enumValue);

        return result;
    }


    /// <summary>
    ///     Get translation value of permission
    ///     We don't have UI to manage permission localizable name. That's why we're using this extension method
    /// </summary>
    /// <param name="permissionRecord">Permission record</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="workContext">Work context</param>
    /// <returns>Translation value</returns>
    public static string GetTranslationPermissionName(this Permission permissionRecord,
        ITranslationService translationService, IWorkContext workContext)
    {
        ArgumentNullException.ThrowIfNull(workContext);

        return GetTranslationPermissionName(permissionRecord, translationService, workContext.WorkingLanguage.Id);
    }

    /// <summary>
    ///     Get translation value of enum
    ///     We don't have UI to manage permission translation name. That's why we're using this extension method
    /// </summary>
    /// <param name="permissionRecord">Permission record</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="languageId">Language identifier</param>
    /// <returns>Translation value</returns>
    public static string GetTranslationPermissionName(this Permission permissionRecord,
        ITranslationService translationService, string languageId)
    {
        ArgumentNullException.ThrowIfNull(permissionRecord);
        ArgumentNullException.ThrowIfNull(translationService);

        //Translation value
        var name = $"Permission.{permissionRecord.SystemName}";
        var result = translationService.GetResource(name, languageId, "", true);

        //set default value if required
        if (string.IsNullOrEmpty(result))
            result = permissionRecord.Name;

        return result;
    }

    /// <summary>
    ///     Save translation name of a permission
    /// </summary>
    /// <param name="permissionRecord">Permission record</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="languageService">Language service</param>
    public static async Task SaveTranslationPermissionName(this Permission permissionRecord,
        ITranslationService translationService, ILanguageService languageService)
    {
        ArgumentNullException.ThrowIfNull(permissionRecord);
        ArgumentNullException.ThrowIfNull(translationService);
        ArgumentNullException.ThrowIfNull(languageService);

        var name = $"Permission.{permissionRecord.SystemName}";
        var value = permissionRecord.Name;

        foreach (var lang in await languageService.GetAllLanguages(true))
        {
            var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
            if (lsr == null)
            {
                lsr = new TranslationResource {
                    LanguageId = lang.Id,
                    Name = name,
                    Value = value
                };
                await translationService.InsertTranslateResource(lsr);
            }
            else
            {
                lsr.Value = value;
                await translationService.UpdateTranslateResource(lsr);
            }
        }
    }

    /// <summary>
    ///     Delete a translation name of a permission
    /// </summary>
    /// <param name="permissionRecord">Permission record</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="languageService">Language service</param>
    public static async Task DeleteTranslationPermissionName(this Permission permissionRecord,
        ITranslationService translationService, ILanguageService languageService)
    {
        ArgumentNullException.ThrowIfNull(permissionRecord);
        ArgumentNullException.ThrowIfNull(translationService);
        ArgumentNullException.ThrowIfNull(languageService);

        var name = $"Permission.{permissionRecord.SystemName}";
        foreach (var lang in await languageService.GetAllLanguages(true))
        {
            var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
            if (lsr != null)
                await translationService.DeleteTranslateResource(lsr);
        }
    }

    /// <summary>
    ///     Delete a translation resource
    /// </summary>
    /// <param name="plugin">Plugin</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="languageService">Language service</param>
    /// <param name="name">Name</param>
    public static async Task DeletePluginTranslationResource(this BasePlugin plugin,
        ITranslationService translationService, ILanguageService languageService,
        string name)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(translationService);
        ArgumentNullException.ThrowIfNull(languageService);
        if (!string.IsNullOrEmpty(name))
            name = name.ToLowerInvariant();
        foreach (var lang in await languageService.GetAllLanguages(true))
        {
            var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
            if (lsr != null)
                await translationService.DeleteTranslateResource(lsr);
        }
    }

    /// <summary>
    ///     Add a translate resource (if new) or update an existing one
    /// </summary>
    /// <param name="plugin">Plugin</param>
    /// <param name="translationService">Translation service</param>
    /// <param name="languageService">Language service</param>
    /// <param name="name">Name</param>
    /// <param name="value">Value</param>
    /// <param name="area">Area</param>
    /// <param name="languageCulture">Language culture code. If null or empty, then a resource will be added for all languages</param>
    public static async Task AddOrUpdatePluginTranslateResource(this BasePlugin plugin,
        ITranslationService translationService, ILanguageService languageService,
        string name, string value, TranslationResourceArea area = TranslationResourceArea.Common,
        string languageCulture = null)
    {
        //actually plugin instance is not required
        ArgumentNullException.ThrowIfNull(plugin);
        ArgumentNullException.ThrowIfNull(translationService);
        ArgumentNullException.ThrowIfNull(languageService);
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
}