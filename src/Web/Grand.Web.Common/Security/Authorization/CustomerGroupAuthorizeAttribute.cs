using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Security.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CustomerGroupAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public CustomerGroupAuthorizeAttribute(string group)
    {
        GroupName = group;
    }

    public string GroupName { get; set; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(GroupName))
            return;

        var groupService = context.HttpContext.RequestServices.GetRequiredService<IGroupService>();
        var workContext = context.HttpContext.RequestServices.GetRequiredService<IWorkContext>();

        if (await groupService.IsInCustomerGroup(workContext.CurrentCustomer, GroupName))
            return;

        var result = (context.HttpContext.Request.ContentType?.Contains("application/json") ?? false) ||
                     context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json",
                         StringComparison.InvariantCultureIgnoreCase) ||
                     context.HttpContext.Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase);

        if (result)
            context.Result = new ObjectResult("") { StatusCode = 403 };
        else
            context.Result = new RedirectToRouteResult("Login", new { returnUrl = context.HttpContext.Request.Path });
    }
}