using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.Security.Captcha;

namespace Grand.Web.Admin.Extensions
{
    public static class SecuritySettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.SecuritySettingsModel ToModel(this CaptchaSettings entity)
        {
            return entity.MapTo<CaptchaSettings, GeneralCommonSettingsModel.SecuritySettingsModel>();
        }
        public static CaptchaSettings ToEntity(this GeneralCommonSettingsModel.SecuritySettingsModel model, CaptchaSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}