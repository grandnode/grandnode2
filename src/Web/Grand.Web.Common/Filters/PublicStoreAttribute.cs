using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Domain.Data;
using Grand.Domain.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents a filter attribute that confirms access to public store
    /// </summary>
    public class PublicStoreAttribute : TypeFilterAttribute
    {
        private readonly bool _ignoreFilter;

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        /// <param name="ignore">Whether to ignore the execution of filter actions</param>
        public PublicStoreAttribute(bool ignore = false) : base(typeof(AccessPublicStoreFilter))
        {
            _ignoreFilter = ignore;
            Arguments = new object[] { ignore };
        }

        public bool IgnoreFilter => _ignoreFilter;

        #region Filter

        /// <summary>
        /// Represents a filter that confirms access to public store
        /// </summary>
        private class AccessPublicStoreFilter : IAsyncAuthorizationFilter
        {
            #region Fields

            private readonly bool _ignoreFilter;
            private readonly IPermissionService _permissionService;
            private readonly StoreInformationSettings _storeInformationSettings;
            #endregion

            #region Ctor

            public AccessPublicStoreFilter(bool ignoreFilter, IPermissionService permissionService, StoreInformationSettings storeInformationSettings)
            {
                _ignoreFilter = ignoreFilter;
                _permissionService = permissionService;
                _storeInformationSettings = storeInformationSettings;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called early in the filter pipeline to confirm request is authorized
            /// </summary>
            /// <param name="filterContext">Authorization filter context</param>
            public async Task OnAuthorizationAsync(AuthorizationFilterContext filterContext)
            {
                //ignore filter (the action available even when navigation is not allowed)
                if (filterContext == null)
                    throw new ArgumentNullException(nameof(filterContext));

                //check whether this filter has been overridden for the Action
                var actionFilter = filterContext.ActionDescriptor.FilterDescriptors
                    .Where(f => f.Scope == FilterScope.Action)
                    .Select(f => f.Filter).OfType<PublicStoreAttribute>().FirstOrDefault();


                //ignore filter (the action is available even if navigation is not allowed)
                if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                    return;

                if (!DataSettingsManager.DatabaseIsInstalled())
                    return;

                //check whether current customer has access to a public store
                if (await _permissionService.Authorize(StandardPermission.PublicStoreAllowNavigation))
                    return;

                if (_storeInformationSettings.StoreClosed)
                {
                    filterContext.Result = new RedirectToRouteResult("StoreClosed", new RouteValueDictionary());
                }
                else
                    //customer has not access to a public store
                    filterContext.Result = new RedirectToRouteResult("Login", new RouteValueDictionary());

            }

            #endregion
        }

        #endregion
    }
}