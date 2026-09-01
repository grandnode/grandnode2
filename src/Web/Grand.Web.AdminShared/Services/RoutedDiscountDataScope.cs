#nullable enable

using Grand.Domain.Discounts;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{Discount}" /> implementation at
///     request time, based on the current request's "area" route value — same fix and same reason
///     as every other Routed*DataScope in this initiative: Grand.Web (the combined host) loads
///     Admin and Store together in one DI container, so a plain per-host registration would
///     silently let whichever host's StartupApplication ran last win for every area in that
///     process.
///
///     Unlike most Routed*DataScope siblings, the Admin branch resolves to the bespoke
///     <see cref="AdminDiscountDataScope"/>, not the generic <see cref="GlobalAdminDataScope{TEntity}"/>
///     — see AdminDiscountDataScope's own doc comment for why. No Vendor branch: Discount has no
///     Vendor screen at all, so any "Vendor" (or other unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedDiscountDataScope(
    IHttpContextAccessor httpContextAccessor,
    AdminDiscountDataScope adminScope,
    StoreAdminDataScope<Discount> storeScope) : IAdminDataScope<Discount>
{
    private IAdminDataScope<Discount> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => adminScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedDiscountDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(Discount entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(Discount entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
