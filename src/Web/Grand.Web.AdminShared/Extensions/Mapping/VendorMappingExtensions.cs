using Grand.Domain.Vendors;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Vendors;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class VendorMappingExtensions
{
    public static VendorModel ToModel(this Vendor entity)
    {
        return entity.MapTo<Vendor, VendorModel>();
    }

    public static Vendor ToEntity(this VendorModel model)
    {
        return model.MapTo<VendorModel, Vendor>();
    }

    public static Vendor ToEntity(this VendorModel model, Vendor destination)
    {
        return model.MapTo(destination);
    }
}