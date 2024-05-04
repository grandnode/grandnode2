using Grand.Infrastructure.Mapper;
using Grand.Web.Vendor.Models.Vendor;

namespace Grand.Web.Vendor.Extensions;

public static class VendorMappingExtensions
{
    public static VendorModel ToModel(this Domain.Vendors.Vendor entity)
    {
        var vendor = entity.MapTo<Domain.Vendors.Vendor, VendorModel>();
        return vendor;
    }

    public static Domain.Vendors.Vendor ToEntity(this VendorModel model, Domain.Vendors.Vendor destination)
    {
        var product = model.MapTo(destination);
        return product;
    }
}