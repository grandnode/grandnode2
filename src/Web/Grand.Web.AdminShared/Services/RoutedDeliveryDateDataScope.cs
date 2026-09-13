#nullable enable

using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

public class RoutedDeliveryDateDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<DeliveryDate> globalScope,
    StoreDeliveryDateDataScope storeScope) : IAdminDataScope<DeliveryDate>
{
    private IAdminDataScope<DeliveryDate> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedDeliveryDateDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(DeliveryDate entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(DeliveryDate entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
