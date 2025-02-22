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
///     Represents a filter attribute confirming that user with "Vendor" customer group has appropriate vendor account
///     associated (and active)
/// </summary>
public class AuthorizeVendorAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public AuthorizeVendorAttribute(bool ignore = false) : base(typeof(AuthorizeVendorFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    /// <summary>
    ///     Represents a filter confirming that user with "Vendor" customer group has appropriate vendor account associated
    ///     (and active)
    /// </summary>
    private class AuthorizeVendorFilter : IAsyncAuthorizationFilter
    {
        #region Ctor

        public AuthorizeVendorFilter(bool ignoreFilter, IContextAccessor contextAccessor, IGroupService groupService,
            IPermissionService permissionService)
        {
            _ignoreFilter = ignoreFilter;
            _contextAccessor = contextAccessor;
            _groupService = groupService;
            _permissionService = permissionService;
        }

        #endregion

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
            if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                return;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //authorize permission of access to the vendor area
            if (!await _permissionService.Authorize(StandardPermission.ManageAccessVendorPanel))
                context.Result = new RedirectToRouteResult("VendorLogin", new RouteValueDictionary());

            //ensure that this user has active vendor record associated
            if (!await _groupService.IsVendor(_contextAccessor.WorkContext.CurrentCustomer) || _contextAccessor.WorkContext.CurrentVendor == null)
                context.Result = new RedirectToRouteResult("VendorLogin", new RouteValueDictionary());
        }

        #endregion

        #region Fields

        private readonly bool _ignoreFilter;
        private readonly IPermissionService _permissionService;
        private readonly IContextAccessor _contextAccessor;
        private readonly IGroupService _groupService;

        #endregion
    }

    #endregion
}