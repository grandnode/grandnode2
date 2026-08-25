#nullable enable

using Grand.Domain.Orders;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Order}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedProductDataScope"/>/<see cref="RoutedCategoryDataScope"/>/
///     <see cref="RoutedCollectionDataScope"/>: <c>Grand.Web</c> (the combined host) loads all
///     three hosts into one DI container, so a plain per-host registration would let whichever
///     host's StartupApplication ran last win for every area in that process.
///
///     First 3-branch routed scope in ARCH-001 — every prior entity's Vendor branch either didn't
///     exist (Category/Collection) or reused the same scope shape as Product's. Order genuinely
///     needs all three.
/// </summary>
public class RoutedOrderDataScope(
    IHttpContextAccessor httpContextAccessor,
    AdminOrderDataScope adminScope,
    StoreOrderDataScope storeScope,
    VendorOrderDataScope vendorScope) : IAdminDataScope<Order>
{
    private IAdminDataScope<Order> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Store" => storeScope,
                "Vendor" => vendorScope,
                //fail closed: this object fronts store/vendor/Sales-Manager tenant isolation, so an
                //unrecognized or missing area must never silently resolve to any concrete scope
                _ => throw new InvalidOperationException(
                    $"RoutedOrderDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Order entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(Order entity) => Resolved.CanView(entity);
    public IEnumerable<OrderItem> FilterOrderItems(IEnumerable<OrderItem> orderItems) =>
        Resolved.FilterOrderItems(orderItems);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
