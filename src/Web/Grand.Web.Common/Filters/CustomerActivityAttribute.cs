using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters
{
    /// <summary>
    /// Represents filter attribute that saves last customer activity date
    /// </summary>
    public class CustomerActivityAttribute : TypeFilterAttribute
    {
        /// <summary>
        /// Create instance of the filter attribute
        /// </summary>
        public CustomerActivityAttribute() : base(typeof(CustomerActivityFilter))
        {
        }

        #region Filter

        /// <summary>
        /// Represents a filter that saves last customer activity date
        /// </summary>
        private class CustomerActivityFilter : IAsyncActionFilter
        {
            #region Fields

            private readonly ICustomerService _customerService;
            private readonly IWorkContext _workContext;
            private readonly IUserFieldService _userFieldService;
            private readonly ICustomerActivityService _customerActivityService;
            private readonly CustomerSettings _customerSettings;

            #endregion

            #region Ctor

            public CustomerActivityFilter(
                ICustomerService customerService,
                IWorkContext workContext,
                IUserFieldService userFieldService,
                ICustomerActivityService customerActivityService,
                CustomerSettings customerSettings)
            {
                _customerService = customerService;
                _workContext = workContext;
                _userFieldService = userFieldService;
                _customerActivityService = customerActivityService;
                _customerSettings = customerSettings;
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called before the action executes, after model binding is complete
            /// </summary>
            /// <param name="context">A context for action filters</param>
            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                await next();
                if (context?.HttpContext?.Request == null)
                    return;

                if (!DataSettingsManager.DatabaseIsInstalled())
                    return;

                //only in GET requests
                if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                    return;

                //update last activity date
                if (_workContext.CurrentCustomer.LastActivityDateUtc.AddMinutes(1.0) < DateTime.UtcNow)
                {
                    await _customerService.UpdateCustomerField(_workContext.CurrentCustomer, x => x.LastActivityDateUtc, DateTime.UtcNow);
                }

                //get current IP address
                var currentIpAddress = context.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                //update customer's IP address
                if (!string.IsNullOrEmpty(currentIpAddress) && !currentIpAddress.Equals(_workContext.CurrentCustomer.LastIpAddress, StringComparison.OrdinalIgnoreCase))
                {
                    _workContext.CurrentCustomer.LastIpAddress = currentIpAddress;
                    await _customerService.UpdateCustomerField(_workContext.CurrentCustomer, x => x.LastIpAddress, currentIpAddress);
                }

                //whether is need to store last visited page URL
                if (!_customerSettings.StoreLastVisitedPage)
                    return;

                //get current page
                var pageUrl = context.HttpContext?.Request?.GetDisplayUrl();
                if (string.IsNullOrEmpty(pageUrl))
                    return;

                //get previous last page
                var previousPageUrl = _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastVisitedPage);

                //save new one if don't match
                if (!pageUrl.Equals(previousPageUrl, StringComparison.OrdinalIgnoreCase))
                    await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.LastVisitedPage, pageUrl);

                if (context.HttpContext.Request?.GetTypedHeaders().Referer?.ToString() is { } referer && !referer.Contains(context.HttpContext.Request.Host.ToString()))
                {
                    var previousUrlReferrer = await _workContext.CurrentCustomer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.LastUrlReferrer);
                    if (previousUrlReferrer != referer)
                    {
                        await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.LastUrlReferrer, referer);
                    }
                }

                if (_customerSettings.SaveVisitedPage)
                {
                    if (!_workContext.CurrentCustomer.IsSearchEngineAccount())
                    {
                        //activity
                        _ = _customerActivityService.InsertActivity("PublicStore.Url", pageUrl, _workContext.CurrentCustomer,
                            context.HttpContext?.Connection?.RemoteIpAddress?.ToString(), pageUrl);
                    }
                }
            }
            #endregion
        }

        #endregion
    }
}