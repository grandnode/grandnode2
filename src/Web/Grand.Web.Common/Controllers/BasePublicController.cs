using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Common.Controllers;

[PublicStore]
[ClosedStore]
[Language]
[Affiliate]
[SharedKernel.Attributes.ApiController]
public abstract class BasePublicController : BaseController
{
    protected IActionResult InvokeHttp404()
    {
        Response.StatusCode = 404;
        return new EmptyResult();
    }

    private bool IsJsonResponseView()
    {
        if (Request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase))
            return Request.Headers.Accept.ToString()
                       .Contains("application/json", StringComparison.InvariantCultureIgnoreCase) ||
                   Request.Headers.Accept.ToString().Equals("*/*", StringComparison.InvariantCultureIgnoreCase);

        if (Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase))
            return Request.ContentType?.Contains("application/json") ?? false;

        return false;
    }

    [IgnoreApi]
    public new IActionResult View(object model)
    {
        if (IsJsonResponseView())
            return Ok(model);

        return base.View(model);
    }

    [IgnoreApi]
    public new IActionResult View(string viewName, object model)
    {
        if (IsJsonResponseView())
            return Json(model);

        return base.View(viewName, model);
    }

    public override RedirectToRouteResult RedirectToRoute(string routeName)
    {
        return IsJsonResponseView() ? RedirectToRoute("Route", new { routeName }) : base.RedirectToRoute(routeName);
    }
}