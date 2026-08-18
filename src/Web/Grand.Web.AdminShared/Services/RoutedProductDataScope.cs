#nullable enable

using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Product}" /> implementation at
///     request time, based on the current request's "area" route value, instead of relying on
///     DI registration order.
///
///     Why this exists: Grand.Web (the combined host, run as "grand-web" in Aspire) references
///     Grand.Web.Admin, Grand.Web.Store, and Grand.Web.Vendor together in one process/one DI
///     container. Each host's own StartupApplication used to register
///     AddScoped&lt;IAdminDataScope&lt;Product&gt;, X&gt;() directly - plain AddScoped doesn't replace an
///     earlier registration, it appends one, so whichever host's StartupApplication ran last (by
///     IStartupApplication.Priority - Vendor's is highest) silently won for every area in that
///     process. That surfaced as a NullReferenceException in VendorProductDataScope.DefaultVendorId
///     when an Admin user opened the product list under the combined host, because Vendor's scope
///     assumes WorkContext.CurrentVendor is set.
///
///     Fix: register the three concrete scopes as themselves (not as IAdminDataScope&lt;Product&gt;)
///     and register this resolver as the single IAdminDataScope&lt;Product&gt; - see
///     Grand.Web.AdminShared/Startup/StartupApplication.cs. Each host's own StartupApplication no
///     longer registers IAdminDataScope&lt;Product&gt; at all.
/// </summary>
public class RoutedProductDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<Product> globalScope,
    StoreAdminDataScope<Product> storeScope,
    VendorProductDataScope vendorScope) : IAdminDataScope<Product>
{
    private IAdminDataScope<Product> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                "Vendor" => vendorScope,
                //fail closed: this object fronts vendor/store tenant isolation, so an unrecognized
                //or missing area must never silently resolve to the unscoped global scope
                _ => throw new InvalidOperationException(
                    $"RoutedProductDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Product entity) => Resolved.HasAccess(entity);

    public Task<bool> CanView(Product entity) => Resolved.CanView(entity);

    public IQueryable<Product> ApplyScope(IQueryable<Product> query) => Resolved.ApplyScope(query);

    public string? DefaultStoreId => Resolved.DefaultStoreId;

    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;

    public bool ShowStoreSelector => Resolved.ShowStoreSelector;

    public string? DefaultVendorId => Resolved.DefaultVendorId;

    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
