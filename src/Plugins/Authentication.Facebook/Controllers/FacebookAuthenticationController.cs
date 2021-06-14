using Authentication.Facebook.Models;
using Grand.Business.Authentication.Interfaces;
using Grand.Business.Authentication.Utilities;
using Grand.SharedKernel;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authentication.Facebook.Controllers
{
    public class FacebookAuthenticationController : BasePluginController
    {
        #region Fields

        private readonly FacebookExternalAuthSettings _facebookExternalAuthSettings;
        private readonly IExternalAuthenticationService _externalAuthenticationService;

        #endregion

        #region Ctor

        public FacebookAuthenticationController(FacebookExternalAuthSettings facebookExternalAuthSettings,
            IExternalAuthenticationService externalAuthenticationService)
        {
            _facebookExternalAuthSettings = facebookExternalAuthSettings;
            _externalAuthenticationService = externalAuthenticationService;
        }

        #endregion

        #region Methods

        public IActionResult FacebookLogin(string returnUrl)
        {
            if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(FacebookAuthenticationDefaults.ProviderSystemName))
                throw new GrandException("Facebook authentication module cannot be loaded");

            if (string.IsNullOrEmpty(_facebookExternalAuthSettings.ClientKeyIdentifier) || string.IsNullOrEmpty(_facebookExternalAuthSettings.ClientSecret))
                throw new GrandException("Facebook authentication module not configured");

            //configure login callback action
            var authenticationProperties = new AuthenticationProperties {
                RedirectUri = Url.Action("FacebookLoginCallback", "FacebookAuthentication", new { returnUrl = returnUrl })
            };

            return Challenge(authenticationProperties, FacebookDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> FacebookLoginCallback(string returnUrl)
        {
            //authenticate Facebook user
            var authenticateResult = await HttpContext.AuthenticateAsync(FacebookDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || !authenticateResult.Principal.Claims.Any())
                return RedirectToRoute("Login");

            //create external authentication parameters
            var authenticationParameters = new ExternalAuthParam {
                ProviderSystemName = FacebookAuthenticationDefaults.ProviderSystemName,
                AccessToken = await HttpContext.GetTokenAsync(FacebookDefaults.AuthenticationScheme, "access_token"),
                Email = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Email)?.Value,
                Identifier = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value,
                Name = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Name)?.Value,
                Claims = authenticateResult.Principal.Claims.ToList()
            };

            //authenticate Grand user
            return await _externalAuthenticationService.Authenticate(authenticationParameters, returnUrl);
        }
        public IActionResult FacebookSignInFailed(string error_code, string error_message, string state)
        {
            //handle exception and display message to user
            var model = new FailedModel() {
                ErrorCode = error_code,
                ErrorMessage = error_message
            };
            return View(model);
        }


        #endregion
    }
}