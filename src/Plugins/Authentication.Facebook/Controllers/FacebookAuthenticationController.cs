using Authentication.Facebook.Models;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.SharedKernel;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Grand.Business.Core.Utilities.Authentication;

namespace Authentication.Facebook.Controllers
{
    public class FacebookAuthenticationController : BasePluginController
    {
        #region Fields

        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly IConfiguration _configuration;

        #endregion

        #region Ctor

        public FacebookAuthenticationController(
            IExternalAuthenticationService externalAuthenticationService,
            IConfiguration configuration)
        {
            _externalAuthenticationService = externalAuthenticationService;
            _configuration = configuration;
        }

        #endregion

        #region Methods

        public IActionResult FacebookLogin(string returnUrl)
        {
            if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(FacebookAuthenticationDefaults.ProviderSystemName))
                throw new GrandException("Facebook authentication module cannot be loaded");

            if (string.IsNullOrEmpty(_configuration["FacebookSettings:AppId"]) || string.IsNullOrEmpty(_configuration["FacebookSettings:AppSecret"]))
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