using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Shipping;

namespace Grand.Web.Admin.Extensions
{
    public static class ShippingSettingsMappingExtensions
    {
        public static ShippingSettingsModel ToModel(this ShippingSettings entity)
        {
            return entity.MapTo<ShippingSettings, ShippingSettingsModel>();
        }
        public static ShippingSettings ToEntity(this ShippingSettingsModel model, ShippingSettings destination)
        {
            return model.MapTo(destination);
        }
    }
}