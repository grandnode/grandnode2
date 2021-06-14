using Authentication.Google.Models;
using Grand.Business.Authentication.Interfaces;
using Grand.Business.Authentication.Utilities;
using Grand.SharedKernel;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Authentication.Google.Controllers
{
    public class GoogleAuthenticationController : BasePluginController
    {
        #region Fields

        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly GoogleExternalAuthSettings _googleExternalAuthSettings;
        #endregion

        #region Ctor

        public GoogleAuthenticationController(IExternalAuthenticationService externalAuthenticationService,
            GoogleExternalAuthSettings googleExternalAuthSettings)
        {
            _externalAuthenticationService = externalAuthenticationService;
            _googleExternalAuthSettings = googleExternalAuthSettings;
        }

        #endregion

        #region Methods

        public IActionResult GoogleLogin(string returnUrl)
        {
            if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(GoogleAuthenticationDefaults.ProviderSystemName))
                throw new GrandException("Google authentication module cannot be loaded");

            if (string.IsNullOrEmpty(_googleExternalAuthSettings.ClientKeyIdentifier) || string.IsNullOrEmpty(_googleExternalAuthSettings.ClientSecret))
                throw new GrandException("Google authentication module not configured");

            //configure login callback action
            var authenticationProperties = new AuthenticationProperties {
                RedirectUri = Url.Action("GoogleLoginCallback", "GoogleAuthentication", new { returnUrl = returnUrl })
            };

            return Challenge(authenticationProperties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleLoginCallback(string returnUrl)
        {
            //authenticate google user
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded || !authenticateResult.Principal.Claims.Any())
                return RedirectToRoute("Login");

            //create external authentication parameters
            var authenticationParameters = new ExternalAuthParam {
                ProviderSystemName = GoogleAuthenticationDefaults.ProviderSystemName,
                AccessToken = await HttpContext.GetTokenAsync(GoogleDefaults.AuthenticationScheme, "access_token"),
                Email = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Email)?.Value,
                Identifier = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value,
                Name = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Name)?.Value,
                Claims = authenticateResult.Principal.Claims.ToList()
            };

            //authenticate grand user
            return await _externalAuthenticationService.Authenticate(authenticationParameters, returnUrl);
        }

        public IActionResult GoogleSignInFailed(string error_message)
        {
            //handle exception and display message to user
            var model = new FailedModel() {
                ErrorMessage = error_message
            };
            return View(model);
        }
        #endregion
    }
}