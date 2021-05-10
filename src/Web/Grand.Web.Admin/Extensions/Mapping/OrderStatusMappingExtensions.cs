using Grand.Domain.Orders;
using Grand.Infrastructure.Mapper;
using Grand.Web.Admin.Models.Orders;

namespace Grand.Web.Admin.Extensions
{
    public static class OrderStatusMappingExtensions
    {
        public static OrderStatusModel ToModel(this OrderStatus entity)
        {
            return entity.MapTo<OrderStatus, OrderStatusModel>();
        }

        public static OrderStatus ToEntity(this OrderStatusModel model)
        {
            return model.MapTo<OrderStatusModel, OrderStatus>();
        }

        public static OrderStatus ToEntity(this OrderStatusModel model, OrderStatus destination)
        {
            return model.MapTo(destination);
        }
    }
}