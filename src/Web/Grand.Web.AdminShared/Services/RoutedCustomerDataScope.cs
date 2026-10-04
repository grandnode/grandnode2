#nullable enable

using Grand.Domain.Customers;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Customer}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedGiftVoucherDataScope" />: Grand.Web (the combined host) loads Admin and
///     Store together in one DI container, so a plain per-host registration would silently let
///     whichever host's StartupApplication ran last win for every area in that process.
///
///     No Vendor branch: Vendor has no CustomerController at all, so "Vendor" (or any other
///     unrecognized/missing area) fails closed. Explicitly forwards CanView rather than relying on
///     the interface default — the standing lesson from VendorReview's own RoutedOrderDataScope
///     gap (a routed scope that forwards HasAccess but not CanView silently reintroduces the
///     interface's HasAccess-only default even when a concrete scope overrides CanView).
/// </summary>
public class RoutedCustomerDataScope(
    IHttpContextAccessor httpContextAccessor,
    AdminCustomerDataScope adminScope,
    StoreCustomerDataScope storeScope) : IAdminDataScope<Customer>
{
    private IAdminDataScope<Customer> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Store" => storeScope,
                //fail closed: this object fronts store tenant isolation and the Sales-Manager
                //restriction, so an unrecognized or missing area (including "Vendor" - Customer has
                //no Vendor screen) must never silently resolve to any concrete scope
                _ => throw new InvalidOperationException(
                    $"RoutedCustomerDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Customer entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(Customer entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
