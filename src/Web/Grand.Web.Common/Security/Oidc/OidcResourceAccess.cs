using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;

namespace Grand.Web.Common.Security.Oidc;

/// <summary>
///     Account-level conditions a protected resource can set on top of the endpoint's own permission checks
/// </summary>
public class OidcResourceAccess
{
    private readonly IPermissionService _permissionService;
    private readonly IGroupService _groupService;

    public OidcResourceAccess(IPermissionService permissionService, IGroupService groupService)
    {
        _permissionService = permissionService;
        _groupService = groupService;
    }

    /// <returns>null when allowed, otherwise a fixed description</returns>
    public async Task<string> Refusal(OidcProtectedResource resource, Customer customer)
    {
        if (resource.DenyVendors && (!string.IsNullOrEmpty(customer.VendorId) || await _groupService.IsVendor(customer)))
            return "Vendor accounts cannot use this resource";

        if (resource.DenyStoreManagers &&
            (!string.IsNullOrEmpty(customer.StaffStoreId) || await _groupService.IsStoreManager(customer)))
            return "Store manager accounts cannot use this resource";

        if (resource.RequireAdminPanelAccess &&
            !await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer))
            return "The account has no access to the administration panel";

        return null;
    }
}
