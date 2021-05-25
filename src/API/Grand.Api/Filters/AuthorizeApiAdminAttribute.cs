using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Data;
using Grand.Domain.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Api.Filters
{
    /// <summary>
    /// Represents a filter attribute that confirms access to the admin panel
    /// </summary>
    public class AuthorizeApiAdminAttribute : TypeFilterAttribute
    {
        private readonly bool _ignoreFilter;

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        /// <param name="ignore">Whether to ignore the execution of filter actions</param>
        public AuthorizeApiAdminAttribute(bool ignore = false) : base(typeof(AuthorizeApiAdminFilter))
        {
            _ignoreFilter = ignore;
            Arguments = new object[] { ignore };
        }

        public bool IgnoreFilter => _ignoreFilter;

        #region Filter

        /// <summary>
        /// Represents a filter that confirms access to the admin panel
        /// </summary>
        private class AuthorizeApiAdminFilter : IAsyncAuthorizationFilter
        {
            #region Fields

            private readonly bool _ignoreFilter;
            private readonly IPermissionService _permissionService;
            private readonly SecuritySettings _securitySettings;

            #endregion

            #region Ctor

            public AuthorizeApiAdminFilter(bool ignoreFilter, IPermissionService permissionService, SecuritySettings securitySettings)
            {
                _ignoreFilter = ignoreFilter;
                _permissionService = permissionService;
                _securitySettings = securitySettings;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called early in the filter pipeline to confirm request is authorized
            /// </summary>
            /// <param name="filterContext">Authorization filter context</param>
            public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
            {

                if (filterContext == null)
                    throw new ArgumentNullException(nameof(filterContext));

                //check whether this filter has been overridden for the action
                var actionFilter = filterContext.ActionDescriptor.FilterDescriptors
                    .Where(f => f.Scope == FilterScope.Action)
                    .Select(f => f.Filter).OfType<AuthorizeApiAdminAttribute>().FirstOrDefault();

                //ignore filter (the action is available even if a customer hasn't access to the admin area)
                if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                    return;

                if (!DataSettingsManager.DatabaseIsInstalled())
                    return;

                //there is AdminAuthorizeFilter, so check access
                if (filterContext.Filters.Any(filter => filter is AuthorizeApiAdminFilter))
                {
                    //authorize permission of access to the admin area
                    if (!await _permissionService.Authorize(StandardPermission.AccessAdminPanel))
                        filterContext.Result = new ForbidResult(JwtBearerDefaults.AuthenticationScheme);

                    //get allowed IP addresses
                    var ipAddresses = _securitySettings.AdminAreaAllowedIpAddresses;

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
}