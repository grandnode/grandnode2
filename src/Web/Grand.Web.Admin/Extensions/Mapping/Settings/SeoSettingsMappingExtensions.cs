using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class SeoSettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.SeoSettingsModel ToModel(this SeoSettings entity)
        {
            return entity.MapTo<SeoSettings, GeneralCommonSettingsModel.SeoSettingsModel>();
        }
        public static SeoSettings ToEntity(this GeneralCommonSettingsModel.SeoSettingsModel model, SeoSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}