using Grand.Domain.Blogs;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

public static class BlogSettingsMappingExtensions
{
    public static ContentSettingsModel.BlogSettingsModel ToModel(this BlogSettings entity)
    {
        return entity.MapTo<BlogSettings, ContentSettingsModel.BlogSettingsModel>();
    }

    public static BlogSettings ToEntity(this ContentSettingsModel.BlogSettingsModel model, BlogSettings destination)
    {
        return model.MapTo(destination);
    }
}