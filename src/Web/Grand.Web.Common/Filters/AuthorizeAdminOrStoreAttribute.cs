using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Data;
using Grand.Domain.Security;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents a filter attribute that grants access to users who have access to either
///     the admin panel (Grand.Web.Admin) or the store manager panel (Grand.Web.Store).
/// </summary>
public class AuthorizeAdminOrStoreAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public AuthorizeAdminOrStoreAttribute(bool ignore = false) : base(typeof(AuthorizeAdminOrStoreFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    private class AuthorizeAdminOrStoreFilter(
        bool ignoreFilter,
        IPermissionService permissionService,
        SecuritySettings securitySettings,
        IContextAccessor contextAccessor,
        IGroupService groupService) : IAsyncAuthorizationFilter
    {
        #region Methods

        public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
        {
            ArgumentNullException.ThrowIfNull(filterContext);

            //check whether this filter has been overridden for the action
            var actionFilter = filterContext.ActionDescriptor.FilterDescriptors
                .Where(f => f.Scope == FilterScope.Action)
                .Select(f => f.Filter).OfType<AuthorizeAdminOrStoreAttribute>().FirstOrDefault();

            if (actionFilter?.IgnoreFilter ?? ignoreFilter)
                return;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            var customer = contextAccessor.WorkContext.CurrentCustomer;
            var isStoreManager = await groupService.IsStoreManager(customer);
            var hasStaffStore = !string.IsNullOrEmpty(customer.StaffStoreId);

            // Store manager path: user belongs to the store manager group with an active store assignment
            if (isStoreManager)
            {
                if (!hasStaffStore || !await permissionService.Authorize(StandardPermission.ManageAccessStoreManagerPanel))
                    filterContext.Result = new RedirectToRouteResult("StoreLogin", new RouteValueDictionary());
                return;
            }

            // Admin path: user is a regular admin (not a vendor)
            if (!await permissionService.Authorize(StandardPermission.ManageAccessAdminPanel))
            {
                filterContext.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());
                return;
            }

            if (await groupService.IsVendor(customer) || contextAccessor.WorkContext.CurrentVendor is not null)
            {
                filterContext.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());
                return;
            }

            // IP address restriction check (admin only)
            var ipAddresses = securitySettings.AdminAreaAllowedIpAddresses;
            if (ipAddresses == null || !ipAddresses.Any())
                return;

            var currentIp = filterContext.HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!ipAddresses.Any(ip => ip.Equals(currentIp, StringComparison.OrdinalIgnoreCase)))
                filterContext.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());
        }

        #endregion
    }

    #endregion
}
