using Grand.Business.Common.Interfaces.Stores;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Link;
using Grand.Web.Common.Models;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;

namespace Widgets.Slider
{
    public static class MyExtensions
    {
        public static SlideModel ToModel(this PictureSlider entity)
        {
            return entity.MapTo<PictureSlider, SlideModel>();
        }

        public static PictureSlider ToEntity(this SlideModel model)
        {
            return model.MapTo<SlideModel, PictureSlider>();
        }


        public static SlideListModel ToListModel(this PictureSlider entity)
        {
            return entity.MapTo<PictureSlider, SlideListModel>();
        }

        public static List<TranslationEntity> ToLocalizedProperty<T>(this IList<T> list) where T : ILocalizedModelLocal
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
        public static bool HasProperty(this Type obj, string propertyName)
        {
            return obj.GetProperty(propertyName) != null;
        }
    }


}