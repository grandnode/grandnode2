using Grand.Domain.Localization;
using Grand.Infrastructure.Models;
using System.Reflection;

namespace Grand.Web.Common.Extensions;

public static class TranslationExtension
{
    public static List<TranslationEntity> ToTranslationProperty<T>(this IList<T> list) where T : ILocalizedModelLocal
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
                    if (i.HasProperty(prop.Name))
                        insert = false;

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
    private static bool HasProperty(this Type obj, string propertyName)
    {
        return obj.GetProperty(propertyName) != null;
    }
}