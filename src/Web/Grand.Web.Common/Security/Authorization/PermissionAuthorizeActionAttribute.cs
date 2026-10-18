using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Security.Authorization;

[AttributeUsage(AttributeTargets.Method)]
public class PermissionAuthorizeActionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public PermissionAuthorizeActionAttribute(string actionName)
    {
        PermissionAction = actionName;
    }

    // Get or set the permission property by manipulating
    public string PermissionAction { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        //ignore filter (the action available even when navigation is not allowed)
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(PermissionAction))
            return;

        //check whether PermissionAuthorizeAttribute filter exists
        var permissionAuthorize = context.Filters
            .OfType<PermissionAuthorizeAttribute>().FirstOrDefault();

        if (permissionAuthorize == null || string.IsNullOrEmpty(permissionAuthorize.Permission))
            return;

        var permissionService = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        var requestPermissionService = context.HttpContext.RequestServices.GetRequiredService<IRequestPermissionService>();
        var contextAccessor = context.HttpContext.RequestServices.GetRequiredService<IContextAccessor>();

        //check whether current customer has access to a public store
        var customer = contextAccessor.WorkContext?.CurrentCustomer;
        if (customer != null &&
            await permissionService.AuthorizeAction(permissionAuthorize.Permission, PermissionAction, customer))
            return;

        //authorize permission of access to the admin area
        if (!await requestPermissionService.Authorize(StandardPermission.ManageAccessAdminPanel))
        {
            context.Result = new RedirectToRouteResult("AdminLogin", new RouteValueDictionary());
        }
        else
        {
            var isAjaxCall = context.HttpContext.Request.Headers["x-requested-with"] == "XMLHttpRequest";
            if (!isAjaxCall)
                context.Result = new RedirectToActionResult("AccessDenied", "Home",
                    new { pageUrl = context.HttpContext.Request.Path });
            else
                context.Result = new JsonResult(new DataSourceResult
                    { Errors = $"Access denied to the resource {context.HttpContext.Request.Path}" });
        }
    }
}