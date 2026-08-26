#nullable enable

using Grand.Domain.Payments;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{PaymentTransaction}" />
///     implementation at request time, based on the current request's "area" route value — same
///     fix and same reason as <see cref="RoutedCategoryDataScope"/>/<see cref="RoutedCollectionDataScope"/>:
///     <c>Grand.Web</c> (the combined host) <c>ProjectReference</c>s <c>Grand.Web.Admin</c> and
///     <c>Grand.Web.Store</c> directly and discovers every <c>IStartupApplication</c> via assembly
///     scan, so both hosts' <c>StartupApplication.ConfigureServices</c> calls run in the one
///     process — a plain per-host <c>AddScoped&lt;IAdminDataScope&lt;PaymentTransaction&gt;, X&gt;()</c>
///     would let whichever host's registration ran last silently win for both areas.
///
///     No Vendor branch: <c>Grand.Web.Vendor</c> has no <c>PaymentTransactionController</c> at all,
///     so any "Vendor" (or other unrecognized/missing) area value fails closed, same shape as
///     <see cref="RoutedCategoryDataScope"/>.
/// </summary>
public class RoutedPaymentTransactionDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<PaymentTransaction> adminScope,
    StorePaymentTransactionDataScope storeScope) : IAdminDataScope<PaymentTransaction>
{
    private IAdminDataScope<PaymentTransaction> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Store" => storeScope,
                //fail closed: this object fronts store tenant isolation, so an unrecognized or
                //missing area (including "Vendor" - PaymentTransaction has no Vendor screen) must
                //never silently resolve to the unscoped global scope
                _ => throw new InvalidOperationException(
                    $"RoutedPaymentTransactionDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(PaymentTransaction entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(PaymentTransaction entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
