using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Data;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents a filter attribute that confirms access to a closed store
    /// </summary>
    public class ClosedStoreAttribute : TypeFilterAttribute
    {
        private readonly bool _ignoreFilter;
        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        /// <param name="ignore">Whether to ignore the execution of filter actions</param>
        public ClosedStoreAttribute(bool ignore = false) : base(typeof(CheckAccessClosedStoreFilter))
        {
            _ignoreFilter = ignore;
            Arguments = new object[] { ignore };
        }

        public bool IgnoreFilter => _ignoreFilter;

        #region Filter

        /// <summary>
        /// Represents a filter that confirms access to closed store
        /// </summary>
        private class CheckAccessClosedStoreFilter : IAsyncActionFilter
        {
            #region Fields

            private readonly bool _ignoreFilter;
            private readonly IPermissionService _permissionService;
            private readonly IWorkContext _workContext;
            private readonly IPageService _pageService;
            private readonly StoreInformationSettings _storeInformationSettings;

            #endregion

            #region Ctor

            public CheckAccessClosedStoreFilter(bool ignoreFilter,
                IPermissionService permissionService,
                IWorkContext workContext,
                IPageService pageService,
                StoreInformationSettings storeInformationSettings)
            {
                _ignoreFilter = ignoreFilter;
                _permissionService = permissionService;
                _workContext = workContext;
                _pageService = pageService;
                _storeInformationSettings = storeInformationSettings;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called before the action executes, after model binding is complete
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {

                if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
                {
                    await next();
                    return;
                }
                //check whether this filter has been overridden for the Action
                var actionFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(f => f.Scope == FilterScope.Action)
                    .Select(f => f.Filter).OfType<ClosedStoreAttribute>().FirstOrDefault();

                if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                {
                    await next();
                    return;
                }

                if (!DataSettingsManager.DatabaseIsInstalled())
                {
                    await next();
                    return;
                }

                //store isn't closed
                if (!_storeInformationSettings.StoreClosed)
                {
                    await next();
                    return;
                }

                //get action and controller names
                var actionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
                var actionName = actionDescriptor?.ActionName;
                var controllerName = actionDescriptor?.ControllerName;

                if ((string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(controllerName)) ||
                    (controllerName.Equals("Common", StringComparison.OrdinalIgnoreCase) &&
                    actionName.Equals("StoreClosed", StringComparison.OrdinalIgnoreCase)))
                {
                    await next();
                    return;
                }

                //pages accessible when a store is closed
                if (controllerName.Equals("Page", StringComparison.OrdinalIgnoreCase) &&
                    actionName.Equals("PageDetails", StringComparison.OrdinalIgnoreCase))
                {
                    //get identifiers of pages are accessible when a store is closed
                    var now = DateTime.UtcNow;
                    var allowedPageIds = (await _pageService.GetAllPages(_workContext.CurrentStore.Id))
                        .Where(t => t.AccessibleWhenStoreClosed &&
                        (!t.StartDateUtc.HasValue || t.StartDateUtc < now) && (!t.EndDateUtc.HasValue || t.EndDateUtc > now))
                        .Select(page => page.Id);

                    //check whether requested page is allowed
                    var requestedPageId = context.RouteData.Values["pageId"] as string;
                    if (!string.IsNullOrEmpty(requestedPageId) && allowedPageIds.Contains(requestedPageId))
                    {
                        await next();
                        return;
                    }
                }

                //check whether current customer has access to a closed store
                if (await _permissionService.Authorize(StandardPermission.AccessClosedStore))
                {
                    await next();
                    return;
                }

                //store is closed and no access, so redirect to 'StoreClosed' page
                context.Result = new RedirectToRouteResult("StoreClosed", new RouteValueDictionary());
            }

            #endregion
        }

        #endregion
    }
}