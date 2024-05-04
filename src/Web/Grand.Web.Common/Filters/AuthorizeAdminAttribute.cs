using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Data;
using Grand.Domain.Security;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents a filter attribute that confirms access to the admin panel
/// </summary>
public class AuthorizeAdminAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public AuthorizeAdminAttribute(bool ignore = false) : base(typeof(AuthorizeAdminFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    /// <summary>
    ///     Represents a filter that confirms access to the admin panel
    /// </summary>
    private class AuthorizeAdminFilter : IAsyncAuthorizationFilter
    {
        #region Ctor

        public AuthorizeAdminFilter(bool ignoreFilter, IPermissionService permissionService,
            SecuritySettings securitySettings, IWorkContext workContext, IGroupService groupService)
        {
            _ignoreFilter = ignoreFilter;
            _permissionService = permissionService;
            _securitySettings = securitySettings;
            _workContext = workContext;
            _groupService = groupService;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Called early in the filter pipeline to confirm request is authorized
        /// </summary>
        /// <param name="filterContext">Authorization filter context</param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
        {
            ArgumentNullException.ThrowIfNull(filterContext);

            //check whether this filter has been overridden for the action
            var actionFilter = filterContext.ActionDescriptor.FilterDescriptors
                .Where(f => f.Scope == FilterScope.Action)
                .Select(f => f.Filter).OfType<AuthorizeAdminAttribute>().FirstOrDefault();

            //ignore filter (the action is available even if a customer hasn't access to the admin area)
            if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                return;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //there is AdminAuthorizeFilter, so check access
            if (filterContext.Filters.Any(filter => filter is AuthorizeAdminFilter))
            {
                //authorize permission of access to the admin area
                if (!await _permissionService.Authorize(StandardPermission.ManageAccessAdminPanel))
                    filterContext.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());

                //whether current customer is vendor
                if (await _groupService.IsVendor(_workContext.CurrentCustomer) ||
                    _workContext.CurrentVendor is not null)
                    filterContext.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());

                //get allowed IP addresses
                var ipAddresses = _securitySettings.AdminAreaAllowedIpAddresses;

                //there are no restrictions
                if (ipAddresses == null || !ipAddresses.Any())
                    return;

                //whether current IP is allowed
                var currentIp = filterContext.HttpContext.Connection.RemoteIpAddress?.ToString();
                if (!ipAddresses.Any(ip => ip.Equals(currentIp, StringComparison.OrdinalIgnoreCase)))
                    filterContext.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());
            }
        }

        #endregion

        #region Fields

        private readonly bool _ignoreFilter;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;

        private readonly SecuritySettings _securitySettings;

        #endregion
    }

    #endregion
}