#nullable enable

using Grand.Domain.Orders;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{MerchandiseReturn}" />
///     implementation at request time, based on the current request's "area" route value — same
///     fix and same reason as <see cref="RoutedOrderDataScope" />/<see cref="RoutedShipmentDataScope" />:
///     <c>Grand.Web</c> (the combined host) <c>ProjectReference</c>s Admin, Store, and Vendor
///     directly and loads all three <c>StartupApplication</c>s into one DI container, so a plain
///     per-host registration would let whichever host's StartupApplication ran last win for every
///     area in that process.
/// </summary>
public class RoutedMerchandiseReturnDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<MerchandiseReturn> adminScope,
    StoreMerchandiseReturnDataScope storeScope,
    VendorMerchandiseReturnDataScope vendorScope) : IAdminDataScope<MerchandiseReturn>
{
    private IAdminDataScope<MerchandiseReturn> Resolved
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
                    $"RoutedMerchandiseReturnDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(MerchandiseReturn entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(MerchandiseReturn entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
