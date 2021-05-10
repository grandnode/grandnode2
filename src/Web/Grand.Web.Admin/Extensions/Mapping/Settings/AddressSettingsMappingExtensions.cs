using Grand.Infrastructure.Mapper;
using Grand.Domain.Common;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class AddressSettingsMappingExtensions
    {
        public static CustomerSettingsModel.AddressSettingsModel ToModel(this AddressSettings entity)
        {
            return entity.MapTo<AddressSettings, CustomerSettingsModel.AddressSettingsModel>();
        }
        public static AddressSettings ToEntity(this CustomerSettingsModel.AddressSettingsModel model, AddressSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}