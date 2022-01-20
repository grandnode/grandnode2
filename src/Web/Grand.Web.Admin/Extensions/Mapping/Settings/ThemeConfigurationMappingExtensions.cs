using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;
using Grand.Web.Common.Themes;

namespace Grand.Web.Admin.Extensions
{
    public static class ThemeConfigurationMappingExtensions
    {
        public static GeneralCommonSettingsModel.StoreInformationSettingsModel.ThemeConfigurationModel ToModel(this ThemeConfiguration entity, string defaultStoreTheme)
        {
            var result = entity.MapTo<ThemeConfiguration, GeneralCommonSettingsModel.StoreInformationSettingsModel.ThemeConfigurationModel>();
            result.Selected = result.ThemeName.Equals(defaultStoreTheme, StringComparison.OrdinalIgnoreCase);
            return result;
        }
    }
}