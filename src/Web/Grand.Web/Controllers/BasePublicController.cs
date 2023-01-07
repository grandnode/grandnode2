using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    [PublicStore]
    [ClosedStore]
    [Language]
    [Affiliate]
    public abstract class BasePublicController : BaseController
    {
        protected IActionResult InvokeHttp404()
        {
            Response.StatusCode = 404;
            return new EmptyResult();
        }

        private bool IsJsonResponseView()
        {
            var viewJson = Request?.Headers["X-Response-View"];
            return viewJson?.Equals("Json") ?? false;
        }

        public new IActionResult View(object model)
        {
            if (IsJsonResponseView())
                return Json(model);

            return base.View(model);
        }

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
}
