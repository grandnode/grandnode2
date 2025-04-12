using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class WarehouseMappingExtensions
{
    public static WarehouseModel ToModel(this Warehouse entity)
    {
        return entity.MapTo<Warehouse, WarehouseModel>();
    }

    public static Warehouse ToEntity(this WarehouseModel model)
    {
        return model.MapTo<WarehouseModel, Warehouse>();
    }

    public static Warehouse ToEntity(this WarehouseModel model, Warehouse destination)
    {
        return model.MapTo(destination);
    }
}