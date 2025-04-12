using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class PickupPointMappingExtensions
{
    public static PickupPointModel ToModel(this PickupPoint entity)
    {
        return entity.MapTo<PickupPoint, PickupPointModel>();
    }

    public static PickupPoint ToEntity(this PickupPointModel model)
    {
        return model.MapTo<PickupPointModel, PickupPoint>();
    }

    public static PickupPoint ToEntity(this PickupPointModel model, PickupPoint destination)
    {
        return model.MapTo(destination);
    }
}