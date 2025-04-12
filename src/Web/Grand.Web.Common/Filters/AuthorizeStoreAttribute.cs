using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Data;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents a filter attribute confirming that user with "Vendor" customer group has appropriate store manager account
///     associated (and active)
/// </summary>
public class AuthorizeStoreAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public AuthorizeStoreAttribute(bool ignore = false) : base(typeof(AuthorizeStoreFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    /// <summary>
    ///     Represents a filter confirming that user with "Store" customer group has appropriate store account associated
    ///     (and active)
    /// </summary>
    private class AuthorizeStoreFilter(bool ignoreFilter, IContextAccessor contextAccessor, IGroupService groupService,
        IPermissionService permissionService) : IAsyncAuthorizationFilter
    {
        #region Methods

        /// <summary>
        ///     Called early in the filter pipeline to confirm request is authorized
        /// </summary>
        /// <param name="context">Authorization filter context</param>
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            //ignore filter actions
            ArgumentNullException.ThrowIfNull(context);

            //check whether this filter has been overridden for the Action
            var actionFilter = context.ActionDescriptor.FilterDescriptors
                .Where(f => f.Scope == FilterScope.Action)
                .Select(f => f.Filter).OfType<AuthorizeVendorAttribute>().FirstOrDefault();

            //ignore filter (the action is available even if the current customer isn't a vendor)
            if (actionFilter?.IgnoreFilter ?? ignoreFilter)
                return;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //authorize permission of access to the vendor area
            if (!await permissionService.Authorize(StandardPermission.ManageAccessStoreManagerPanel))
            {
                context.Result = new RedirectToRouteResult("StoreLogin", new RouteValueDictionary());
                return;
            }
            //ensure that this user has store manager
            if (!await groupService.IsStoreManager(contextAccessor.WorkContext.CurrentCustomer) || string.IsNullOrEmpty(contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            {
                context.Result = new RedirectToRouteResult("StoreLogin", new RouteValueDictionary());
            }
            return;
            //add active store
        }

        #endregion
    }

    #endregion
}