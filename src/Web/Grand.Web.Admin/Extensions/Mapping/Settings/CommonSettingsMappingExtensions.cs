using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class CommonSettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.CommonSettingsModel ToModel(this CommonSettings entity)
        {
            return entity.MapTo<CommonSettings, GeneralCommonSettingsModel.CommonSettingsModel>();
        }
        public static CommonSettings ToEntity(this GeneralCommonSettingsModel.CommonSettingsModel model, CommonSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}