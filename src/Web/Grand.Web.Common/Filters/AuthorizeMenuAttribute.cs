using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.System.Admin;
using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents a filter attribute that confirms access by admin menu to the controllers/actions in the admin
/// </summary>
public class AuthorizeMenuAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public AuthorizeMenuAttribute(bool ignore = false) : base(typeof(AuthorizeMenuFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    /// <summary>
    ///     Represents a filter that confirms access to the admin panel
    /// </summary>
    public class AuthorizeMenuFilter : IAsyncAuthorizationFilter
    {
        #region Ctor

        public AuthorizeMenuFilter(
            bool ignoreFilter,
            IPermissionService permissionService,
            IAdminSiteMapService adminSiteMapService,
            IWorkContext workContext,
            SecurityConfig securityConfig)
        {
            _ignoreFilter = ignoreFilter;
            _permissionService = permissionService;
            _adminSiteMapService = adminSiteMapService;
            _workContext = workContext;
            _securityConfig = securityConfig;
        }

        #endregion

        #region Fields

        private readonly bool _ignoreFilter;
        private readonly IPermissionService _permissionService;
        private readonly IAdminSiteMapService _adminSiteMapService;
        private readonly IWorkContext _workContext;
        private readonly SecurityConfig _securityConfig;

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
                .Select(f => f.Filter).OfType<AuthorizeMenuAttribute>().FirstOrDefault();

            //ignore filter (the action is available even if a customer hasn't access to the admin area)
            if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                return;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            if (!_securityConfig.AuthorizeAdminMenu)
                return;

            var controllerName = filterContext.RouteData.Values["controller"]?.ToString();
            var actionName = filterContext.RouteData.Values["action"]?.ToString();

            await ValidateMenuSiteMapPermissions(filterContext, controllerName, actionName);
        }

        private async Task ValidateMenuSiteMapPermissions(AuthorizationFilterContext filterContext,
            string controllerName,
            string actionName)
        {
            var menuSiteMap = await FindSiteMap(controllerName, actionName);
            if (menuSiteMap != null && menuSiteMap.PermissionNames.Any())
            {
                if (menuSiteMap.AllPermissions)
                {
                    if (!await menuSiteMap.PermissionNames.AllAsync(async x =>
                            await _permissionService.Authorize(x, _workContext.CurrentCustomer)))
                        filterContext.Result = new ForbidResult();
                }
                else
                {
                    if (!await menuSiteMap.PermissionNames.AnyAsync(async x =>
                            await _permissionService.Authorize(x, _workContext.CurrentCustomer)))
                        filterContext.Result = new ForbidResult();
                }
            }
        }

        private async Task<AdminSiteMap> FindSiteMap(string controllerName,
            string actionName)
        {
            var adminSiteMap = await _adminSiteMapService.GetSiteMap();
            return adminSiteMap.Select(siteMap =>
                    FindSiteMapByControllerAndActionName(siteMap, controllerName, actionName))
                .FirstOrDefault(siteMap => siteMap != null);
        }

        private static AdminSiteMap FindSiteMapByControllerAndActionName(AdminSiteMap siteMap,
            string controllerName, string actionName)
        {
            if (siteMap.ControllerName == controllerName && siteMap.ActionName == actionName)
                return siteMap;

            return siteMap.ChildNodes
                .Select(child => FindSiteMapByControllerAndActionName(child, controllerName, actionName))
                .FirstOrDefault(result => result != null);
        }

        #endregion
    }

    #endregion
}