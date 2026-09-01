#nullable enable

using Grand.Domain.Messages;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Resolves the correct per-host <see cref="IAdminDataScope{ContactAttribute}" /> implementation
///     at request time, based on the current request's "area" route value — same fix and same reason
///     as <see cref="RoutedCategoryDataScope"/>: Grand.Web (the combined host) loads Admin and Store
///     together in one DI container, so a plain per-host registration would silently let whichever
///     host's StartupApplication ran last win for every area in that process.
///
///     No Vendor branch: ContactAttribute has no Vendor screen at all, so any "Vendor" (or other
///     unrecognized/missing) area value fails closed.
/// </summary>
public class RoutedContactAttributeDataScope(
    IHttpContextAccessor httpContextAccessor,
    GlobalAdminDataScope<ContactAttribute> globalScope,
    StoreAdminDataScope<ContactAttribute> storeScope) : IAdminDataScope<ContactAttribute>
{
    private IAdminDataScope<ContactAttribute> Resolved
    {
        get
        {
            var area = httpContextAccessor.HttpContext?.Request.RouteValues["area"] as string;
            return area switch {
                "Admin" => globalScope,
                "Store" => storeScope,
                _ => throw new InvalidOperationException(
                    $"RoutedContactAttributeDataScope: unrecognized or missing area '{area}'.")
            };
        }
    }

    public Task<bool> HasAccess(ContactAttribute entity) => Resolved.HasAccess(entity);
    public Task<bool> CanView(ContactAttribute entity) => Resolved.CanView(entity);
    public string? DefaultStoreId => Resolved.DefaultStoreId;
    public string ResourceKeyPrefix => Resolved.ResourceKeyPrefix;
    public bool ShowStoreSelector => Resolved.ShowStoreSelector;
    public string? DefaultVendorId => Resolved.DefaultVendorId;
    public bool CanFeatureOnHomepage => Resolved.CanFeatureOnHomepage;
}
