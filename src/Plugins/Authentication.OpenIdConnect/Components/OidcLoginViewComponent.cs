using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.OpenIdConnect.Components;

[ViewComponent(Name = "OidcLogin")]
public class OidcLoginViewComponent(IAuthenticationSchemeProvider authenticationSchemeProvider) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        //no scheme (not configured, or not restarted since the install): a button that can only fail is not shown
        if (await authenticationSchemeProvider.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme) == null)
            return Content(string.Empty);

        return View();
    }
}
