#nullable enable

using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Vendor's <see cref="IAdminDataScope{Order}" />. Bespoke: ownership is over a child
///     collection (any <c>OrderItem.VendorId</c> match), not a flat field on the entity itself —
///     ports the existing <c>HasAccessToOrder</c>/<c>HasAccessToOrderItem</c> extension methods
///     from <c>Grand.Web.Vendor/Extensions/HasAccess.cs</c>. Also the only scope that overrides
///     <see cref="FilterOrderItems" />: a vendor viewing a mixed-vendor order sees only its own
///     line items, ported from Vendor's original
///     <c>order.OrderItems.Where(HasAccessToOrderItem)</c> filter inside
///     <c>PrepareOrderDetailsModel</c>.
/// </summary>
public class VendorOrderDataScope(IContextAccessor contextAccessor) : IAdminDataScope<Order>
{
    public Task<bool> HasAccess(Order entity) =>
        Task.FromResult(entity is not null &&
            entity.OrderItems.Any(i => i.VendorId == contextAccessor.WorkContext.CurrentVendor.Id));

    public IEnumerable<OrderItem> FilterOrderItems(IEnumerable<OrderItem> orderItems) =>
        orderItems.Where(i => i.VendorId == contextAccessor.WorkContext.CurrentVendor.Id);

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Vendor";
    public bool ShowStoreSelector => false;
    public string? DefaultVendorId => contextAccessor.WorkContext.CurrentVendor.Id;
    public bool CanFeatureOnHomepage => false;
}
