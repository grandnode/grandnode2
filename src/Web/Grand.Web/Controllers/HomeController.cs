using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    public class HomeController : BasePublicController
    {
        public virtual IActionResult Index()
        {
            return View();
        }
    }
}
