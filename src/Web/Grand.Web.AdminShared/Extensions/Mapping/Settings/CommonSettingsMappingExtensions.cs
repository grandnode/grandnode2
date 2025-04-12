using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Settings;

namespace Grand.Web.AdminShared.Extensions.Mapping.Settings;

public static class CommonSettingsMappingExtensions
{
    public static GeneralCommonSettingsModel.CommonSettingsModel ToModel(this CommonSettings entity)
    {
        return entity.MapTo<CommonSettings, GeneralCommonSettingsModel.CommonSettingsModel>();
    }

    public static CommonSettings ToEntity(this GeneralCommonSettingsModel.CommonSettingsModel model,
        CommonSettings destination)
    {
        return model.MapTo(destination);
    }
}