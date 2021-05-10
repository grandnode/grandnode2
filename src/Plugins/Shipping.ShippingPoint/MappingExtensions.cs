using Grand.Infrastructure.Mapper;
using Shipping.ShippingPoint.Domain;
using Shipping.ShippingPoint.Models;

namespace Shipping.ShippingPoint
{
    public static class MappingExtensions
    {
        public static ShippingPointModel ToModel(this ShippingPoints entity)
        {
            return entity.MapTo<ShippingPoints, ShippingPointModel>();
        }

        public static ShippingPoints ToEntity(this ShippingPointModel model)
        {
            return model.MapTo<ShippingPointModel, ShippingPoints>();
        }

        public static ShippingPoints ToEntity(this ShippingPointModel model, ShippingPoints destination)
        {
            return model.MapTo(destination);
        }

    }
}
