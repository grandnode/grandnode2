#nullable enable

using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

public class RoutedPickupPointDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<PickupPoint> globalScope,
    StorePickupPointDataScope storeScope) : IAdminDataScope<PickupPoint>
{
    private IAdminDataScope<PickupPoint> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedPickupPointDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(PickupPoint entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(PickupPoint entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
