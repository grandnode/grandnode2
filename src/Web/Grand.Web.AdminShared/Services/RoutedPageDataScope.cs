#nullable enable

using Grand.Domain.Pages;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Page}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedCategoryDataScope"/> (see that file's doc comment): Grand.Web (the
///     combined host) loads Admin and Store together in one DI container, so a plain
///     AddScoped&lt;IAdminDataScope&lt;Page&gt;, X&gt;() per host would silently let whichever
///     host's StartupApplication ran last win for every area in that process.
///
///     No Vendor branch: Vendor has no Page screen at all, so any "Vendor" (or other
///     unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedPageDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<Page> globalScope,
    StoreAdminDataScope<Page> storeScope) : IAdminDataScope<Page>
{
    private IAdminDataScope<Page> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedPageDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Page entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(Page entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
