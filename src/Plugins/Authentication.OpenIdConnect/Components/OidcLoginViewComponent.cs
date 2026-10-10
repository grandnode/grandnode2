using Microsoft.AspNetCore.Mvc;

namespace Authentication.OpenIdConnect.Components;

[ViewComponent(Name = "OidcLogin")]
public class OidcLoginViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}
