#nullable enable

using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Warehouse}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason as
///     <see cref="RoutedCategoryDataScope"/>: Grand.Web (the combined host) loads Admin and Store
///     together in one DI container. No Vendor branch: Vendor has no Shipping screen at all, so any
///     "Vendor" (or other unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedWarehouseDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<Warehouse> globalScope,
    StoreWarehouseDataScope storeScope) : IAdminDataScope<Warehouse>
{
    private IAdminDataScope<Warehouse> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedWarehouseDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Warehouse entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(Warehouse entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
