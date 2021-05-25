using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents a filter attribute confirming that user with "Vendor" customer group has appropriate vendor account associated (and active)
    /// </summary>
    public class AuthorizeVendorAttribute : TypeFilterAttribute
    {
        private readonly bool _ignoreFilter;

        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        /// <param name="ignore">Whether to ignore the execution of filter actions</param>
        public AuthorizeVendorAttribute(bool ignore = false) : base(typeof(AuthorizeVendorFilter))
        {
            _ignoreFilter = ignore;
            Arguments = new object[] { ignore };
        }

        public bool IgnoreFilter => _ignoreFilter;

        #region Filter

        /// <summary>
        /// Represents a filter confirming that user with "Vendor" customer group has appropriate vendor account associated (and active)
        /// </summary>
        private class AuthorizeVendorFilter : IAsyncAuthorizationFilter
        {
            #region Fields

            private readonly bool _ignoreFilter;
            private readonly IWorkContext _workContext;
            private readonly IGroupService _groupService;
            #endregion

            #region Ctor

            public AuthorizeVendorFilter(bool ignoreFilter, IWorkContext workContext, IGroupService groupService)
            {
                _ignoreFilter = ignoreFilter;
                _workContext = workContext;
                _groupService = groupService;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called early in the filter pipeline to confirm request is authorized
            /// </summary>
            /// <param name="filterContext">Authorization filter context</param>
            public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
            {
                //ignore filter actions
                if (context == null)
                    throw new ArgumentNullException(nameof(context));

                //check whether this filter has been overridden for the Action
                var actionFilter = context.ActionDescriptor.FilterDescriptors
                    .Where(f => f.Scope == FilterScope.Action)
                    .Select(f => f.Filter).OfType<AuthorizeVendorAttribute>().FirstOrDefault();

                //ignore filter (the action is available even if the current customer isn't a vendor)
                if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                    return;

                if (!DataSettingsManager.DatabaseIsInstalled())
                    return;

                //whether current customer is vendor
                if (!await _groupService.IsVendor(_workContext.CurrentCustomer))
                    return;

                //ensure that this user has active vendor record associated
                if (_workContext.CurrentVendor == null)
                    context.Result = new ChallengeResult();
            }

            #endregion
        }

        #endregion
    }
}