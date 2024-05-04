using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;

namespace Grand.Web.Vendor.Extensions;

public static class HasAccess
{
    public static bool HasAccessToProduct(this IWorkContext workContext, Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        return product.VendorId == workContext.CurrentVendor.Id;
    }

    public static bool HasAccessToOrder(this IWorkContext workContext, Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        var hasVendorProducts = order.OrderItems.Any(orderItem => orderItem.VendorId == workContext.CurrentVendor.Id);
        return hasVendorProducts;
    }

    public static bool HasAccessToOrderItem(this IWorkContext workContext, OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);

        return orderItem.VendorId == workContext.CurrentVendor.Id;
    }

    public static bool HasAccessToShipment(this IWorkContext workContext, Shipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        return shipment.VendorId == workContext.CurrentVendor.Id;
    }
}