using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Localization;
using Grand.Infrastructure.Mapper;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Models;
using System.Reflection;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;

namespace Widgets.Slider;

public static class MyExtensions
{
    public static SlideModel ToModel(this PictureSlider entity, IDateTimeService dateTimeService)
    {
        var slideModel = entity.MapTo<PictureSlider, SlideModel>();
        slideModel.StartDateUtc = entity.StartDateUtc.ConvertToUserTime(dateTimeService);
        slideModel.EndDateUtc = entity.EndDateUtc.ConvertToUserTime(dateTimeService);
        return slideModel;
    }

    public static PictureSlider ToEntity(this SlideModel model, IDateTimeService dateTimeService)
    {
        var pictureSlider = model.MapTo<SlideModel, PictureSlider>();
        pictureSlider.StartDateUtc = model.StartDateUtc.ConvertToUtcTime(dateTimeService);
        pictureSlider.EndDateUtc = model.EndDateUtc.ConvertToUtcTime(dateTimeService);
        return pictureSlider;
    }

    public static PictureSlider ToEntity(this SlideModel model, PictureSlider destination,
        IDateTimeService dateTimeService)
    {
        var pictureSlider = model.MapTo(destination);
        pictureSlider.StartDateUtc = model.StartDateUtc.ConvertToUtcTime(dateTimeService);
        pictureSlider.EndDateUtc = model.EndDateUtc.ConvertToUtcTime(dateTimeService);
        return pictureSlider;
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

    public static bool HasProperty(this Type obj, string propertyName)
    {
        return obj.GetProperty(propertyName) != null;
    }
}