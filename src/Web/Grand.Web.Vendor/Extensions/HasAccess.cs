using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;

namespace Grand.Web.Vendor.Extensions
{
    public static class HasAccess
    {
        public static bool HasAccessToProduct(this IWorkContext workContext, Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return product.VendorId == workContext.CurrentVendor.Id;
        }

        public static bool HasAccessToOrder(this IWorkContext workContext, Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var hasVendorProducts = order.OrderItems.Any(orderItem => orderItem.VendorId == workContext.CurrentVendor.Id);
            return hasVendorProducts;
        }

        public static bool HasAccessToOrderItem(this IWorkContext workContext, OrderItem orderItem)
        {
            if (orderItem == null)
                throw new ArgumentNullException(nameof(orderItem));

            return orderItem.VendorId == workContext.CurrentVendor.Id;
        }
        public static bool HasAccessToShipment(this IWorkContext workContext, Shipment shipment)
        {
            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));
            
            return shipment.VendorId == workContext.CurrentVendor.Id;
        }
    }
}
