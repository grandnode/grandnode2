using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Web.Common.Models;
using Grand.Domain;
using Grand.Domain.Localization;
using Grand.Domain.Seo;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Extensions
{
    public static class TranslationExtension
    {
        public static List<TranslationEntity> ToTranslationProperty<T>(this IList<T> list) where T : ILocalizedModelLocal
        {
            var local = new List<TranslationEntity>();
            foreach (var item in list)
            {
                Type[] interfaces = item.GetType().GetInterfaces();
                PropertyInfo[] props = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                foreach (var prop in props)
                {
                    bool insert = true;

                    foreach (var i in interfaces)
                    {
                        if (i.HasProperty(prop.Name))
                        {
                            insert = false;
                        }
                    }

                    if (insert && prop.GetValue(item) != null)
                        local.Add(new TranslationEntity()
                        {
                            LanguageId = item.LanguageId,
                            LocaleKey = prop.Name,
                            LocaleValue = prop.GetValue(item).ToString(),
                        });
                }
            }
            return local;
        }

        public static async Task<List<TranslationEntity>> ToTranslationProperty<T, E>(this IList<T> list, E entity, Expression<Func<T, string>> keySelector,
            SeoSettings _seoSettings, ISlugService _slugService, ILanguageService _languageService) where T : ILocalizedModelLocal where E : BaseEntity, ISlugEntity
        {
            var local = new List<TranslationEntity>();
            foreach (var item in list)
            {
                Type[] interfaces = item.GetType().GetInterfaces();
                PropertyInfo[] props = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                foreach (var prop in props)
                {
                    bool insert = true;

                    foreach (var i in interfaces)
                    {
                        if (i.HasProperty(prop.Name) && typeof(ILocalizedModelLocal).IsAssignableFrom(i))
                        {
                            insert = false;
                        }
                        if (i.HasProperty(prop.Name) && typeof(ISlugModelLocal).IsAssignableFrom(i))
                        {
                            var member = keySelector.Body as MemberExpression;
                            if (member == null)
                                throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");

                            var propInfo = member.Member as PropertyInfo;
                            if (propInfo == null)
                                throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");

                            var value = item.GetType().GetProperty(propInfo.Name).GetValue(item, null);
                            if (value != null)
                            {
                                var name = value.ToString();
                                var itemvalue = prop.GetValue(item) ?? "";
                                var seName = await entity.ValidateSeName(itemvalue.ToString(), name, false, _seoSettings, _slugService, _languageService);
                                prop.SetValue(item, seName);
                                await _slugService.SaveSlug(entity, seName, item.LanguageId);
                            }
                            else
                            {
                                var itemvalue = prop.GetValue(item) ?? "";
                                if (itemvalue != null && !string.IsNullOrEmpty(itemvalue.ToString()))
                                {
                                    var seName = await entity.ValidateSeName(itemvalue.ToString(), "", false, _seoSettings, _slugService, _languageService);
                                    prop.SetValue(item, seName);
                                    await _slugService.SaveSlug(entity, seName, item.LanguageId);
                                }
                                else
                                    insert = false;
                            }
                        }
                    }

                    if (insert && prop.GetValue(item) != null)
                        local.Add(new TranslationEntity()
                        {
                            LanguageId = item.LanguageId,
                            LocaleKey = prop.Name,
                            LocaleValue = prop.GetValue(item).ToString(),
                        });
                }
            }
            return local;
        }


        public static bool HasProperty(this Type obj, string propertyName)
        {
            return obj.GetProperty(propertyName) != null;
        }
    }
}
