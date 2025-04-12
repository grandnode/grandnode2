using Grand.Domain.News;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Extensions.Mapping.Settings;

public static class NewsSettingsMappingExtensions
{
    public static ContentSettingsModel.NewsSettingsModel ToModel(this NewsSettings entity)
    {
        return entity.MapTo<NewsSettings, ContentSettingsModel.NewsSettingsModel>();
    }

    public static NewsSettings ToEntity(this ContentSettingsModel.NewsSettingsModel model, NewsSettings destination)
    {
        return model.MapTo(destination);
    }
}