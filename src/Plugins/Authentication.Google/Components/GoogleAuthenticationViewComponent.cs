using Microsoft.AspNetCore.Mvc;

namespace Authentication.Google.Components
{
    [ViewComponent(Name = "GoogleAuthentication")]
    public class GoogleAuthenticationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}