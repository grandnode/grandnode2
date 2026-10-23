using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Web.Store.Filters;

/// <summary>
///     Managing customers from the store panel only makes sense when customer identity is scoped per
///     store (Customer:RegisterCustomersPerStore). When it's off, every action of the customer controller
///     and of its edit tab controllers is routed to Customer/PerStoreDisabled, the page that explains how
///     to enable the setting.
/// </summary>
public class PerStoreCustomersAttribute() : TypeFilterAttribute(typeof(PerStoreCustomersFilter))
{
    public const string DisabledController = "Customer";
    public const string DisabledAction = "PerStoreDisabled";

    public class PerStoreCustomersFilter(CustomerConfig customerConfig) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!customerConfig.RegisterCustomersPerStore && !IsDisabledPage(context.ActionDescriptor))
            {
                context.Result = new RedirectToActionResult(DisabledAction, DisabledController, null);
                return;
            }

            await next();
        }

        private static bool IsDisabledPage(ActionDescriptor descriptor) =>
            descriptor is ControllerActionDescriptor {
                ControllerName: DisabledController, ActionName: DisabledAction
            };
    }
}
