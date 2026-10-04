#nullable enable

using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Collection}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedProductDataScope"/>/<see cref="RoutedCategoryDataScope"/> (see those
///     files' doc comments): Grand.Web (the combined host) loads Admin and Store together in one
///     DI container, so a plain AddScoped&lt;IAdminDataScope&lt;Collection&gt;, X&gt;() per host
///     would silently let whichever host's StartupApplication ran last win for every area in that
///     process.
///
///     Unlike Product, there is no Vendor branch: Vendor has no Collection screen at all, so any
///     "Vendor" (or other unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedCollectionDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<Collection> globalScope,
    StoreAdminDataScope<Collection> storeScope) : IAdminDataScope<Collection>
{
    private IAdminDataScope<Collection> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                //fail closed: this object fronts store tenant isolation, so an unrecognized or
                //missing area (including "Vendor" - Collection has no Vendor screen) must never
                //silently resolve to the unscoped global scope
                _ => throw new InvalidOperationException(
                    $"RoutedCollectionDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Collection entity) => Resolved.HasAccess(entity);

    public Task<bool> CanView(Collection entity) => Resolved.CanView(entity);

    public string? DefaultStoreId => Resolved.DefaultStoreId;

    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;

    public bool ShowStoreSelector => Resolved.ShowStoreSelector;

    public string? DefaultVendorId => Resolved.DefaultVendorId;

    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
