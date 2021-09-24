using Grand.Domain.Stores;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class StoreInformationSettingsMappingExtensions
    {
        public static GeneralCommonSettingsModel.StoreInformationSettingsModel ToModel(this StoreInformationSettings entity)
        {
            return entity.MapTo<StoreInformationSettings, GeneralCommonSettingsModel.StoreInformationSettingsModel>();
        }
        public static StoreInformationSettings ToEntity(this GeneralCommonSettingsModel.StoreInformationSettingsModel model, StoreInformationSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}