#nullable enable

using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Vendor's <see cref="IAdminDataScope{Shipment}" />. Bespoke: ownership is a flat
///     <c>VendorId</c> field directly on the entity, simpler than Order's child-collection
///     ownership — ports the existing <c>HasAccessToShipment</c>/<c>HasAccessToOrderItem</c>
///     extension methods (<c>Grand.Web.Vendor/Extensions/HasAccess.cs</c>) inline, the same way
///     <see cref="VendorOrderDataScope" />/<see cref="VendorProductDataScope" /> do — not imported
///     directly, since <c>Grand.Web.Vendor</c> already references <c>Grand.Web.AdminShared</c> and
///     a reference the other way would be circular. Also overrides <see cref="FilterOrderItems" />,
///     reusing the interface member the Order phase already added: ports Vendor's original
///     <c>order.OrderItems.Where(HasAccessToOrderItem)</c> filter (used when building the
///     AddShipment order-item picker) so a vendor can only ship its own line items on a
///     mixed-vendor order.
/// </summary>
public class VendorShipmentDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Shipment>
{
    public Task<bool> HasAccess(Shipment entity) =>
        Task.FromResult(entity is not null &&
            entity.VendorId == contextAccessor.WorkContext.CurrentVendor.Id);

    public IEnumerable<OrderItem> FilterOrderItems(IEnumerable<OrderItem> orderItems) =>
        orderItems.Where(i => i.VendorId == contextAccessor.WorkContext.CurrentVendor.Id);

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Vendor";
    public bool ShowStoreSelector => false;
    public string? DefaultVendorId => contextAccessor.WorkContext.CurrentVendor.Id;
    public bool CanFeatureOnHomepage => false;
}
