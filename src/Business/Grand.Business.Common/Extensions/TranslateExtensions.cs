using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Grand.Business.Common.Extensions
{
    public static class TranslateExtensions
    {

        /// <summary>
        /// Get translation property of an entity
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
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var member = keySelector.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");

            string result = default;
            string resultStr = string.Empty;

            string localeKeyGroup = typeof(T).Name;
            string localeKey = propInfo.Name;

            if (!String.IsNullOrEmpty(languageId))
            {
                if (entity.Locales.Any())
                {
                    var en = entity.Locales.FirstOrDefault(x => x.LanguageId == languageId && x.LocaleKey == localeKey);
                    if (en != null)
                    {
                        resultStr = en.LocaleValue;
                        if (!String.IsNullOrEmpty(resultStr))
                            result = resultStr;
                    }
                }
            }

            //set default value if required
            if (String.IsNullOrEmpty(resultStr) && returnDefaultValue)
            {
                result = (string)(propInfo.GetValue(entity));
            }
            return result;
        }

        /// <summary>
        /// Get translation value of enum
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
            if (workContext == null)
                throw new ArgumentNullException(nameof(workContext));

            return GetTranslationEnum(enumValue, translationService, workContext.WorkingLanguage.Id);
        }
        /// <summary>
        /// Get translation value of enum
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
            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));

            if (!typeof(T).GetTypeInfo().IsEnum) throw new ArgumentException("T must be enum type");

            //Translation value
            string resourceName = string.Format("Enums.{0}.{1}",
                typeof(T),
                enumValue.ToString());
            string result = translationService.GetResource(resourceName, languageId, "", true);

            //set default value if required
            if (String.IsNullOrEmpty(result))
                result = CommonHelper.ConvertEnum(enumValue.ToString());

            return result;
        }


        /// <summary>
        /// Get translation value of permission
        /// We don't have UI to manage permission localizable name. That's why we're using this extension method
        /// </summary>
        /// <param name="permissionRecord">Permission record</param>
        /// <param name="translationService">Translation service</param>
        /// <param name="workContext">Work context</param>
        /// <returns>Translation value</returns>
        public static string GetTranslationPermissionName(this Permission permissionRecord,
            ITranslationService translationService, IWorkContext workContext)
        {
            if (workContext == null)
                throw new ArgumentNullException(nameof(workContext));

            return GetTranslationPermissionName(permissionRecord, translationService, workContext.WorkingLanguage.Id);
        }
        /// <summary>
        /// Get translation value of enum
        /// We don't have UI to manage permission translation name. That's why we're using this extension method
        /// </summary>
        /// <param name="permissionRecord">Permission record</param>
        /// <param name="translationService">Translation service</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Translation value</returns>
        public static string GetTranslationPermissionName(this Permission permissionRecord,
            ITranslationService translationService, string languageId)
        {
            if (permissionRecord == null)
                throw new ArgumentNullException(nameof(permissionRecord));

            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));

            //Translation value
            string name = $"Permission.{permissionRecord.SystemName}";
            string result = translationService.GetResource(name, languageId, "", true);

            //set default value if required
            if (String.IsNullOrEmpty(result))
                result = permissionRecord.Name;

            return result;
        }

        /// <summary>
        /// Save translation name of a permission
        /// </summary>
        /// <param name="permissionRecord">Permission record</param>
        /// <param name="translationService">Translation service</param>
        /// <param name="languageService">Language service</param>
        public static async Task SaveTranslationPermissionName(this Permission permissionRecord,
            ITranslationService translationService, ILanguageService languageService)
        {
            if (permissionRecord == null)
                throw new ArgumentNullException(nameof(permissionRecord));
            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));
            if (languageService == null)
                throw new ArgumentNullException(nameof(languageService));

            string name = $"Permission.{permissionRecord.SystemName}";
            string value = permissionRecord.Name;

            foreach (var lang in await languageService.GetAllLanguages(true))
            {
                var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
                if (lsr == null)
                {
                    lsr = new TranslationResource
                    {
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
        /// Delete a translation name of a permission
        /// </summary>
        /// <param name="permissionRecord">Permission record</param>
        /// <param name="translationService">Translation service</param>
        /// <param name="languageService">Language service</param>
        public static async Task DeleteTranslationPermissionName(this Permission permissionRecord,
            ITranslationService translationService, ILanguageService languageService)
        {
            if (permissionRecord == null)
                throw new ArgumentNullException(nameof(permissionRecord));
            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));
            if (languageService == null)
                throw new ArgumentNullException(nameof(languageService));

            string name = $"Permission.{permissionRecord.SystemName}";
            foreach (var lang in await languageService.GetAllLanguages(true))
            {
                var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
                if (lsr != null)
                    await translationService.DeleteTranslateResource(lsr);
            }
        }

        /// <summary>
        /// Delete a translation resource
        /// </summary>
        /// <param name="plugin">Plugin</param>
        /// <param name="translationService">Translation service</param>
        /// <param name="languageService">Language service</param>
        /// <param name="name">Name</param>
        public static async Task DeletePluginTranslationResource(this BasePlugin plugin,
            ITranslationService translationService, ILanguageService languageService,
            string name)
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));
            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));
            if (languageService == null)
                throw new ArgumentNullException(nameof(languageService));
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
        /// Add a translate resource (if new) or update an existing one
        /// </summary>
        /// <param name="plugin">Plugin</param>
        /// <param name="translationService">Translation service</param>
        /// <param name="languageService">Language service</param>
        /// <param name="name">Name</param>
        /// <param name="value">Value</param>
        /// <param name="languageCulture">Language culture code. If null or empty, then a resource will be added for all languages</param>
        public static async Task AddOrUpdatePluginTranslateResource(this BasePlugin plugin,
            ITranslationService translationService, ILanguageService languageService,
            string name, string value, string languageCulture = null)
        {
            //actually plugin instance is not required
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));
            if (translationService == null)
                throw new ArgumentNullException(nameof(translationService));
            if (languageService == null)
                throw new ArgumentNullException(nameof(languageService));
            if (string.IsNullOrEmpty(name))
                name = name.ToLowerInvariant();
            foreach (var lang in await languageService.GetAllLanguages(true))
            {
                if (!String.IsNullOrEmpty(languageCulture) && !languageCulture.Equals(lang.LanguageCulture))
                    continue;

                var lsr = await translationService.GetTranslateResourceByName(name, lang.Id);
                if (lsr == null)
                {
                    lsr = new TranslationResource
                    {
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
    }
}
