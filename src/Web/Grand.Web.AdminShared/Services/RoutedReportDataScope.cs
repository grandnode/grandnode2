#nullable enable

using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IReportDataScope" /> implementation at request time,
///     based on the current request's "area" route value — same fix and same reason as
///     <see cref="RoutedOrderDataScope" />/<see cref="RoutedPaymentTransactionDataScope" />:
///     <c>Grand.Web</c> (the combined host) <c>ProjectReference</c>s Admin, Store, and Vendor
///     directly and loads all three <c>StartupApplication</c>s into one DI container, so a plain
///     per-host registration would let whichever host's StartupApplication ran last win for every
///     area in that process.
/// </summary>
public class RoutedReportDataScope(
    IHttpContextAccessor httpContextAccessor,
    AdminReportDataScope adminScope,
    StoreReportDataScope storeScope,
    VendorReportDataScope vendorScope) : IReportDataScope
{
    private IReportDataScope Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Store" => storeScope,
                "Vendor" => vendorScope,
                //fail closed: this object fronts store/vendor tenant isolation, so an unrecognized
                //or missing area must never silently resolve to any concrete scope
                _ => throw new InvalidOperationException(
                    $"RoutedReportDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public string StoreId => Resolved.StoreId;
    public string VendorId => Resolved.VendorId;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public bool ShowVendorSelector => Resolved.ShowVendorSelector;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool CanIncludeProduct(Product product) => Resolved.CanIncludeProduct(product);
}
