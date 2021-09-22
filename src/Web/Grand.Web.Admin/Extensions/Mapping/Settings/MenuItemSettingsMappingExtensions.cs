using Grand.Domain.Common;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class MenuItemSettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.DisplayMenuSettingsModel ToModel(this MenuItemSettings entity)
        {
            return entity.MapTo<MenuItemSettings, GeneralCommonSettingsModel.DisplayMenuSettingsModel>();
        }
        public static MenuItemSettings ToEntity(this GeneralCommonSettingsModel.DisplayMenuSettingsModel model, MenuItemSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}