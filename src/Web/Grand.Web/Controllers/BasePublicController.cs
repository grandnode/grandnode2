using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    [PublicStore]
    [ClosedStore]
    [Language]
    [Affiliate]
    public abstract partial class BasePublicController : BaseController
    {
        protected virtual IActionResult InvokeHttp404()
        {
            Response.StatusCode = 404;
            return new EmptyResult();
        }

        protected bool IsJsonResponseView()
        {
            var viewJson = Request?.Headers["X-Response-View"];
            if (viewJson?.Equals("Json") ?? false)
            {
                return true;
            }
            return false;
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
    }
}
