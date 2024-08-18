using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using Grand.Infrastructure.Models;
using Grand.SharedKernel.Extensions;
using System.Linq.Expressions;
using System.Reflection;

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

    public async Task<List<TranslationEntity>> TranslationSeNameProperties<T, TE>(IList<T> list, TE entity, Expression<Func<T, string>> keySelector)
        where T : ILocalizedModelLocal where TE : BaseEntity, ISlugEntity
    {
        var local = new List<TranslationEntity>();
        foreach (var item in list)
        {
            var interfaces = item.GetType().GetInterfaces();
            var props = item.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (var prop in props)
            {
                var insert = true;

                foreach (var i in interfaces)
                {
                    if (HasProperty(i, prop.Name) && typeof(ILocalizedModelLocal).IsAssignableFrom(i)) insert = false;
                    if (HasProperty(i, prop.Name) && typeof(ISlugModelLocal).IsAssignableFrom(i))
                    {
                        if (keySelector.Body is not MemberExpression member)
                            throw new ArgumentException(
                                $"Expression '{keySelector}' refers to a method, not a property.");

                        var propInfo = member.Member as PropertyInfo;
                        if (propInfo == null)
                            throw new ArgumentException(
                                $"Expression '{keySelector}' refers to a field, not a property.");

                        var value = item.GetType().GetProperty(propInfo.Name)?.GetValue(item, null);
                        if (value != null)
                        {
                            var name = value.ToString();
                            var itemValue = prop.GetValue(item) ?? "";
                            var seName = await ValidateSeName(entity, itemValue.ToString(), name, false);
                            prop.SetValue(item, seName);
                        }
                        else
                        {
                            var itemValue = prop.GetValue(item) ?? "";
                            if (!string.IsNullOrEmpty(itemValue.ToString()))
                            {
                                var seName = await ValidateSeName(entity, itemValue.ToString(), "", false);
                                prop.SetValue(item, seName);
                            }
                            else
                            {
                                insert = false;
                            }
                        }
                    }
                }

                if (insert && prop.GetValue(item) != null)
                    local.Add(new TranslationEntity {
                        LanguageId = item.LanguageId,
                        LocaleKey = prop.Name,
                        LocaleValue = prop.GetValue(item)?.ToString()
                    });
            }
        }

        return local;
    }

    public async Task SaveSeName<T>(T entity) where T : BaseEntity, ISlugEntity
    {
        //save main entity
        await slugService.SaveSlug(entity, entity.SeName, "");
        if (entity is ITranslationEntity translationEntity)
        {
            //save translation entities
            foreach (var locale in translationEntity.Locales
                         .Where(x => x.LocaleKey == nameof(ISlugEntity.SeName) 
                                     && !x.LocaleValue.Equals(entity.SeName, StringComparison.InvariantCulture)))
            {
                await slugService.SaveSlug(entity, locale.LocaleValue, locale.LanguageId);
            }
        }
    }

    private static bool HasProperty(Type obj, string propertyName)
    {
        return obj.GetProperty(propertyName) != null;
    }

    private string GetSeName(string name)
    {
        return SeoExtensions.GenerateSlug(name, seoSettings.ConvertNonWesternChars, seoSettings.AllowUnicodeCharsInUrls,
            seoSettings.AllowSlashChar, seoSettings.SeoCharConversion);
    }
}