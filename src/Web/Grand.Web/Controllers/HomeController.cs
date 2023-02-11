using Grand.Web.Common.Attributes;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    public class HomeController : BasePublicController
    {
        [IgnoreApi]
        [HttpGet]
        public virtual IActionResult Index()
        {
            return View();
        }
    }
}
