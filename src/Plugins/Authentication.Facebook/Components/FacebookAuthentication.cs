using Microsoft.AspNetCore.Mvc;

namespace Authentication.Facebook.Components
{
    [ViewComponent(Name = "FacebookAuthentication")]
    public class FacebookAuthenticationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Authentication.Facebook/Views/PublicInfo.cshtml");
        }
    }
}