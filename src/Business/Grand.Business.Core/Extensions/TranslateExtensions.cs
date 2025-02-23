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
        IContextAccessor contextAccessor)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(contextAccessor);

        return GetTranslationEnum(enumValue, translationService, contextAccessor.WorkContext.WorkingLanguage.Id);
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
    /// <returns>Translation value</returns>
    public static string GetTranslationPermissionName(this Permission permissionRecord)
    {
        return $"Permission.{permissionRecord.SystemName}";
    }
}