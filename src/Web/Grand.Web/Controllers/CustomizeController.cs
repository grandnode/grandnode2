using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    public class CustomizeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
