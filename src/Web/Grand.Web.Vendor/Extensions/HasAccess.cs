using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Vendors;
using Grand.Infrastructure;

namespace Grand.Web.Vendor.Extensions;

/// <summary>
///     Vendor tenant-isolation checks. This is the one place ownership of a domain entity is decided for
///     the Vendor area - always check the loaded entity you are about to read/mutate through one of these
///     methods rather than comparing `entity.VendorId` inline. Checking the entity itself (instead of an
///     incoming request/DTO field) guarantees the object you authorized is the same object you act on -
///     see IProductValidVendor in Grand.Web.Vendor.Models.Catalog for the class of bug that arises when
///     those two are allowed to drift apart.
/// </summary>
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

    public static bool HasAccessToVendorReview(this IWorkContext workContext, VendorReview vendorReview)
    {
        ArgumentNullException.ThrowIfNull(vendorReview);

        return vendorReview.VendorId == workContext.CurrentVendor.Id;
    }
}