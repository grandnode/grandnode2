using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Data;
using Grand.Domain.Security;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Module.Api.Filters;

/// <summary>
///     Represents a filter attribute that confirms access to the admin panel
/// </summary>
public class AuthorizeApiAdminAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public AuthorizeApiAdminAttribute(bool ignore = false) : base(typeof(AuthorizeApiAdminFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    /// <summary>
    ///     Represents a filter that confirms access to the admin panel
    /// </summary>
    private class AuthorizeApiAdminFilter(bool ignoreFilter, IPermissionService permissionService,
        IContextAccessor contextAccessor, IGroupService groupService, SecuritySettings securitySettings, BackendAPIConfig backendApiConfig) : IAsyncAuthorizationFilter
    {

        #region Ctor

        #endregion

        #region Methods

        /// <summary>
        ///     Called early in the filter pipeline to confirm request is authorized
        /// </summary>
        /// <param name="filterContext">Authorization filter context</param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
        {
            ArgumentNullException.ThrowIfNull(filterContext);

            //the kill switch also covers tokens forwarded to the OpenID Connect scheme, which skip the Bearer scheme's
            //check; it applies to every action, including those that ignore the admin area checks below
            if (!backendApiConfig.Enabled)
            {
                filterContext.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
                return;
            }

            //check whether this filter has been overridden for the action
            var actionFilter = filterContext.ActionDescriptor.FilterDescriptors
                .Where(f => f.Scope == FilterScope.Action)
                .Select(f => f.Filter).OfType<AuthorizeApiAdminAttribute>().FirstOrDefault();

            //ignore filter (the action is available even if a customer hasn't access to the admin area)
            if (actionFilter?.IgnoreFilter ?? ignoreFilter)
                return;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //there is AdminAuthorizeFilter, so check access
            if (filterContext.Filters.Any(filter => filter is AuthorizeApiAdminFilter))
            {
                //authorize permission of access to the admin area
                var customer = contextAccessor.WorkContext.CurrentCustomer;
                if (!await permissionService.Authorize(StandardPermission.ManageAccessAdminPanel, customer))
                {
                    filterContext.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
                    return;
                }

                //the API works on all data, so the scoped panel roles stay out, as they do in the admin panel;
                //a sales manager is turned away too, because the API has no sales employee scope
                if (await groupService.IsVendor(customer) || !string.IsNullOrEmpty(customer.VendorId) ||
                    await groupService.IsStoreManager(customer) || !string.IsNullOrEmpty(customer.StaffStoreId) ||
                    await groupService.IsSalesManager(customer))
                {
                    filterContext.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
                    return;
                }

                //get allowed IP addresses
                var ipAddresses = securitySettings.AdminAreaAllowedIpAddresses;

                //there are no restrictions
                if (ipAddresses == null || !ipAddresses.Any())
                    return;

                //whether current IP is allowed
                var currentIp = filterContext.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                if (!ipAddresses.Any(ip => ip.Equals(currentIp, StringComparison.OrdinalIgnoreCase)))
                    filterContext.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
            }
        }

        #endregion
        
    }

    #endregion
}