#nullable enable

using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{VendorReview}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason as
///     <see cref="RoutedProductDataScope" /> (see that file's doc comment): Grand.Web (the combined
///     host) loads Admin, Store, and Vendor together in one DI container, so a plain
///     AddScoped&lt;IAdminDataScope&lt;VendorReview&gt;, X&gt;() per host would silently let whichever
///     host's StartupApplication ran last win for every area in that process.
///
///     Unlike Category/Collection/PaymentTransaction (Admin/Store, no Vendor branch), this is the
///     mirror-image 2-branch case: Admin/Vendor, no Store branch — VendorReview has no Store screen
///     at all.
/// </summary>
public class RoutedVendorReviewDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<VendorReview> adminScope,
    VendorVendorReviewDataScope vendorScope) : IAdminDataScope<VendorReview>
{
    private IAdminDataScope<VendorReview> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Vendor" => vendorScope,
                //fail closed: this object fronts vendor tenant isolation, so an unrecognized or
                //missing area (including "Store" - VendorReview has no Store screen) must never
                //silently resolve to the unscoped global scope
                _ => throw new InvalidOperationException(
                    $"RoutedVendorReviewDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(VendorReview entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(VendorReview entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
