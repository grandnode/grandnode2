#nullable enable

using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{ProductAttribute}" /> implementation
///     at request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedCategoryDataScope"/>: Grand.Web (the combined host) loads Admin and Store
///     together in one DI container, so a plain per-host registration would silently let whichever
///     host's StartupApplication ran last win for every area in that process.
///
///     No Vendor branch: ProductAttribute has no Vendor screen at all, so any "Vendor" (or other
///     unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedProductAttributeDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<ProductAttribute> globalScope,
    StoreAdminDataScope<ProductAttribute> storeScope) : IAdminDataScope<ProductAttribute>
{
    private IAdminDataScope<ProductAttribute> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedProductAttributeDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(ProductAttribute entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(ProductAttribute entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
