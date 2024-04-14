using Grand.Domain.Vendors;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Settings;

namespace Grand.Web.Admin.Extensions.Mapping.Settings;

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