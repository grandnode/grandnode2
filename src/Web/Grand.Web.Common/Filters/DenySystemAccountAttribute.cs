using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents a filter attribute that deny system account to some resources
/// </summary>
public class DenySystemAccountAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    /// <param name="ignore">Whether to ignore the execution of filter actions</param>
    public DenySystemAccountAttribute(bool ignore = false) : base(typeof(DenySystemAccountFilter))
    {
        IgnoreFilter = ignore;
        Arguments = [ignore];
    }

    public bool IgnoreFilter { get; }

    #region Filter

    /// <summary>
    ///     Represents a filter that deny access for system accounts to resources
    /// </summary>
    private class DenySystemAccountFilter : IAsyncAuthorizationFilter
    {
        #region Ctor

        public DenySystemAccountFilter(bool ignoreFilter, IWorkContext workContext)
        {
            _ignoreFilter = ignoreFilter;
            _workContext = workContext;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Called early in the filter pipeline to confirm request is authorized
        /// </summary>
        /// <param name="context">Authorization filter context</param>
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            //ignore filter (the action available even when navigation is not allowed)
            ArgumentNullException.ThrowIfNull(context);

            var actionFilter = context.ActionDescriptor.FilterDescriptors
                .Where(f => f.Scope == FilterScope.Action)
                .Select(f => f.Filter).OfType<DenySystemAccountAttribute>().FirstOrDefault();

            //ignore filter
            if (actionFilter?.IgnoreFilter ?? _ignoreFilter)
                return Task.CompletedTask;

            if (!DataSettingsManager.DatabaseIsInstalled())
                return Task.CompletedTask;

            if (_workContext.CurrentCustomer.IsSystemAccount())
                context.Result = new RedirectToRouteResult("HomePage", new RouteValueDictionary());

            return Task.CompletedTask;
        }

        #endregion

        #region Fields

        private readonly bool _ignoreFilter;
        private readonly IWorkContext _workContext;

        #endregion
    }

    #endregion
}