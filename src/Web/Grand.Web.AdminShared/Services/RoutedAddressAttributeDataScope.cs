#nullable enable

using Grand.Domain.Common;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{AddressAttribute}" /> implementation
///     at request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedCategoryDataScope"/>: Grand.Web (the combined host) loads Admin and Store
///     together in one DI container, so a plain per-host registration would silently let whichever
///     host's StartupApplication ran last win for every area in that process.
///
///     No Vendor branch: AddressAttribute has no Vendor screen at all, so any "Vendor" (or other
///     unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedAddressAttributeDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<AddressAttribute> globalScope,
    StoreAdminDataScope<AddressAttribute> storeScope) : IAdminDataScope<AddressAttribute>
{
    private IAdminDataScope<AddressAttribute> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedAddressAttributeDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(AddressAttribute entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(AddressAttribute entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
