using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;
using System;
using System.Threading.Tasks;

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
            private readonly ICustomerActionEventService _customerActionEventService;
            private readonly CustomerSettings _customerSettings;

            #endregion

            #region Ctor

            public CustomerActivityFilter(
                ICustomerService customerService,
                IWorkContext workContext,
                IUserFieldService userFieldService,
                ICustomerActivityService customerActivityService,
                ICustomerActionEventService customerActionEventService,
                CustomerSettings customerSettings)
            {
                _customerService = customerService;
                _workContext = workContext;
                _userFieldService = userFieldService;
                _customerActivityService = customerActivityService;
                _customerActionEventService = customerActionEventService;
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
                if (context == null || context.HttpContext == null || context.HttpContext.Request == null)
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

                if (!string.IsNullOrEmpty(context.HttpContext.Request.Headers[HeaderNames.Referer]))
                    if (!context.HttpContext.Request.Headers[HeaderNames.Referer].ToString().Contains(context.HttpContext.Request.Host.ToString()))
                    {
                        var previousUrlReferrer = _workContext.CurrentCustomer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.LastUrlReferrer);
                        var actualUrlReferrer = context.HttpContext.Request.Headers[HeaderNames.Referer];
                        if (previousUrlReferrer != actualUrlReferrer)
                        {
                            await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.LastUrlReferrer, actualUrlReferrer);
                        }
                    }

                if (_customerSettings.SaveVisitedPage)
                {
                    if (!_workContext.CurrentCustomer.IsSearchEngineAccount())
                    {
                        //activity
                        await _customerActivityService.InsertActivity("PublicStore.Url", pageUrl, pageUrl, _workContext.CurrentCustomer);
                        //action
                        await _customerActionEventService.Url(_workContext.CurrentCustomer, context.HttpContext?.Request?.Path.ToString(), context.HttpContext?.Request?.Headers["Referer"]);
                    }
                }
            }
            #endregion
        }

        #endregion
    }
}