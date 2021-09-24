using Grand.Domain.Seo;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class GoogleAnalyticsSettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.GoogleAnalyticsSettingsModel ToModel(this GoogleAnalyticsSettings entity)
        {
            return entity.MapTo<GoogleAnalyticsSettings, GeneralCommonSettingsModel.GoogleAnalyticsSettingsModel>();
        }
        public static GoogleAnalyticsSettings ToEntity(this GeneralCommonSettingsModel.GoogleAnalyticsSettingsModel model, GoogleAnalyticsSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}