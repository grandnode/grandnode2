using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Common.Filters;

/// <summary>
///     Represents filter attribute that saves last customer activity date
/// </summary>
public class CustomerActivityAttribute : TypeFilterAttribute
{
    /// <summary>
    ///     Create instance of the filter attribute
    /// </summary>
    public CustomerActivityAttribute() : base(typeof(CustomerActivityFilter))
    {
    }

    #region Filter

    /// <summary>
    ///     Represents a filter that saves last customer activity date
    /// </summary>
    private class CustomerActivityFilter(
        ICustomerService customerService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings) : IAsyncActionFilter
    {
        #region Methods

        /// <summary>
        ///     Called before the action executes, after model binding is complete
        /// </summary>
        /// <param name="context">A context for action filters</param>
        /// <param name="next"></param>
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();

            if (!DataSettingsManager.DatabaseIsInstalled())
                return;

            //only in GET requests
            if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                return;

            //whether is need to store last visited page URL
            if (!customerSettings.StoreLastVisitedPage)
                return;

            //update last activity date
            if (contextAccessor.WorkContext.CurrentCustomer.LastActivityDateUtc.AddMinutes(3.0) < DateTime.UtcNow)
                await customerService.UpdateCustomerField(contextAccessor.WorkContext.CurrentCustomer, x => x.LastActivityDateUtc,
                    DateTime.UtcNow);

            //get current IP address
            var currentIpAddress = context.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            //update customer's IP address
            if (!string.IsNullOrEmpty(currentIpAddress) &&
                !currentIpAddress.Equals(contextAccessor.WorkContext.CurrentCustomer.LastIpAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                contextAccessor.WorkContext.CurrentCustomer.LastIpAddress = currentIpAddress;
                await customerService.UpdateCustomerField(contextAccessor.WorkContext.CurrentCustomer, x => x.LastIpAddress,
                    currentIpAddress);
            }

            //get current page
            var pageUrl = context.HttpContext?.Request?.GetDisplayUrl();
            if (string.IsNullOrEmpty(pageUrl))
                return;

            //save new one if don't match
            if (!pageUrl.Equals(contextAccessor.WorkContext.CurrentCustomer.LastVisitedPage, StringComparison.OrdinalIgnoreCase))
            {
                contextAccessor.WorkContext.CurrentCustomer.LastVisitedPage = pageUrl;
                await customerService.UpdateCustomerField(contextAccessor.WorkContext.CurrentCustomer, x => x.LastVisitedPage, pageUrl);
            }
        }

        #endregion
    }

    #endregion
}