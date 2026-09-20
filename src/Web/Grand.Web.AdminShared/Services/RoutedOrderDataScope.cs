#nullable enable

using Grand.Domain.Orders;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;
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
