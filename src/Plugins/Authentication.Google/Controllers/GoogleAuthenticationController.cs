using Authentication.Google.Models;
using Grand.Business.Authentication.Interfaces;
using Grand.Business.Authentication.Utilities;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Infrastructure;
using Grand.SharedKernel;
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

        private readonly GoogleExternalAuthSettings _googleExternalAuthSettings;
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public GoogleAuthenticationController(GoogleExternalAuthSettings googleExternalAuthSettings,
            IExternalAuthenticationService externalAuthenticationService,
            ITranslationService translationService,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            _googleExternalAuthSettings = googleExternalAuthSettings;
            _externalAuthenticationService = externalAuthenticationService;
            _translationService = translationService;
            _permissionService = permissionService;
            _settingService = settingService;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area("Admin")]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                ClientKeyIdentifier = _googleExternalAuthSettings.ClientKeyIdentifier,
                ClientSecret = _googleExternalAuthSettings.ClientSecret
            };

            return View("~/Plugins/Authentication.Google/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [AuthorizeAdmin]
        [Area("Admin")]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            _googleExternalAuthSettings.ClientKeyIdentifier = model.ClientKeyIdentifier;
            _googleExternalAuthSettings.ClientSecret = model.ClientSecret;
            await _settingService.SaveSetting(_googleExternalAuthSettings);

            //now clear settings cache
            await _settingService.ClearCache();

            Success(_translationService.GetResource("Admin.Plugins.Saved"));

            return await Configure();

        }


        public IActionResult GoogleLogin(string returnUrl)
        {
            if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(GoogleAuthenticationDefaults.ProviderSystemName))
                throw new GrandException("Google authentication module cannot be loaded");

            if (string.IsNullOrEmpty(_googleExternalAuthSettings.ClientKeyIdentifier) || string.IsNullOrEmpty(_googleExternalAuthSettings.ClientSecret))
                throw new GrandException("Google authentication module not configured");

            //configure login callback action
            var authenticationProperties = new AuthenticationProperties
            {
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
            var authenticationParameters = new ExternalAuthParam
            {
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
            var model = new FailedModel()
            {
                ErrorMessage = error_message
            };
            return View("~/Plugins/Authentication.Google/Views/SignInFailed.cshtml", model);
        }
        #endregion
    }
}