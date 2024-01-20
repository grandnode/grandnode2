using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
