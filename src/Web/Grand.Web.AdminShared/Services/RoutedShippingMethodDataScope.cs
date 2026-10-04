#nullable enable

using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

public class RoutedShippingMethodDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<ShippingMethod> globalScope,
    StoreShippingMethodDataScope storeScope) : IAdminDataScope<ShippingMethod>
{
    private IAdminDataScope<ShippingMethod> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedShippingMethodDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(ShippingMethod entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(ShippingMethod entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
