using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.RequestCustomerResolvers;
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
    internal class CustomerActivityFilter(
        ICustomerService customerService,
        IContextAccessor contextAccessor,
        CustomerSettings customerSettings,
        IGuestCustomerPersistence guestCustomerPersistence) : IAsyncActionFilter
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

            //system accounts are shared by every request they serve; an unsaved guest is saved by a write,
            //which activity tracking must not trigger
            var customer = contextAccessor.WorkContext.CurrentCustomer;
            if (customer.IsSystemAccount || guestCustomerPersistence.IsTransient(customer.Id))
                return;

            var changed = false;

            //update last activity date
            if (customer.LastActivityDateUtc.AddMinutes(3.0) < DateTime.UtcNow)
            {
                customer.LastActivityDateUtc = DateTime.UtcNow;
                changed = true;
            }

            //get current IP address
            var currentIpAddress = context.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            //update customer's IP address
            if (!string.IsNullOrEmpty(currentIpAddress) &&
                !currentIpAddress.Equals(customer.LastIpAddress,
                    StringComparison.OrdinalIgnoreCase))
            {
                customer.LastIpAddress = currentIpAddress;
                changed = true;
            }

            //get current page; save new one if don't match
            var pageUrl = context.HttpContext?.Request?.GetDisplayUrl();
            if (!string.IsNullOrEmpty(pageUrl) &&
                !pageUrl.Equals(customer.LastVisitedPage, StringComparison.OrdinalIgnoreCase))
            {
                customer.LastVisitedPage = pageUrl;
                changed = true;
            }

            //one write for all changed fields
            if (changed)
                await customerService.UpdateCustomerLastActivity(customer);
        }

        #endregion
    }

    #endregion
}