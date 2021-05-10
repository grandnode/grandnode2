using Grand.Infrastructure.Mapper;
using Grand.Domain.Vendors;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions
{
    public static class VendorSettingsMappingExtensions
    {
        public static VendorSettingsModel ToModel(this VendorSettings entity)
        {
            return entity.MapTo<VendorSettings, VendorSettingsModel>();
        }
        public static VendorSettings ToEntity(this VendorSettingsModel model, VendorSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}