#nullable enable

using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Shipment}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedOrderDataScope"/>/<see cref="RoutedProductDataScope"/>: <c>Grand.Web</c>
///     (the combined host) loads all three hosts into one DI container, so a plain per-host
///     registration would let whichever host's StartupApplication ran last win for every area in
///     that process.
/// </summary>
public class RoutedShipmentDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<Shipment> adminScope,
    StoreShipmentDataScope storeScope,
    VendorShipmentDataScope vendorScope) : IAdminDataScope<Shipment>
{
    private IAdminDataScope<Shipment> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Store" => storeScope,
                "Vendor" => vendorScope,
                //fail closed: this object fronts store/vendor tenant isolation, so an
                //unrecognized or missing area must never silently resolve to any concrete scope
                _ => throw new InvalidOperationException(
                    $"RoutedShipmentDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Shipment entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(Shipment entity) => Resolved.CanView(entity);
    public IEnumerable<OrderItem> FilterOrderItems(IEnumerable<OrderItem> orderItems) =>
        Resolved.FilterOrderItems(orderItems);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
