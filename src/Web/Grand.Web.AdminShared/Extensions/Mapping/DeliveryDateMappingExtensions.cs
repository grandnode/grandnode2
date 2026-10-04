using Grand.Domain.Shipping;
using Grand.Infrastructure.Mapper;
using Grand.Web.AdminShared.Models.Shipping;

namespace Grand.Web.AdminShared.Extensions.Mapping;

public static class DeliveryDateMappingExtensions
{
    public static DeliveryDateModel ToModel(this DeliveryDate entity)
    {
        return entity.MapTo<DeliveryDate, DeliveryDateModel>();
    }

    public static DeliveryDate ToEntity(this DeliveryDateModel model)
    {
        return model.MapTo<DeliveryDateModel, DeliveryDate>();
    }

    public static DeliveryDate ToEntity(this DeliveryDateModel model, DeliveryDate destination)
    {
        return model.MapTo(destination);
    }
}