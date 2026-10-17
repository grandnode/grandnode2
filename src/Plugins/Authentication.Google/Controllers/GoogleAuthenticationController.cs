using Authentication.Google.Models;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Authentication.Google.Controllers;

public class GoogleAuthenticationController : BasePluginController
{
    #region Ctor

    public GoogleAuthenticationController(IExternalAuthenticationService externalAuthenticationService,
        IConfiguration configuration,
        IContextAccessor contextAccessor)
    {
        _externalAuthenticationService = externalAuthenticationService;
        _configuration = configuration;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Fields

    private readonly IExternalAuthenticationService _externalAuthenticationService;
    private readonly IConfiguration _configuration;
    private readonly IContextAccessor _contextAccessor;

    #endregion

    #region Methods

    public IActionResult GoogleLogin(string returnUrl)
    {
        if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(
                GoogleAuthenticationDefaults.ProviderSystemName, _contextAccessor.WorkContext.CurrentCustomer,
                _contextAccessor.StoreContext.CurrentStore))
            throw new GrandException("Google authentication module cannot be loaded");

        if (string.IsNullOrEmpty(_configuration["GoogleSettings:ClientId"]) ||
            string.IsNullOrEmpty(_configuration["GoogleSettings:ClientSecret"]))
            throw new GrandException("Google authentication module not configured");

        //configure login callback action
        var authenticationProperties = new AuthenticationProperties {
            RedirectUri = Url.Action("GoogleLoginCallback", "GoogleAuthentication", new { returnUrl })
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
            Identifier =
                authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value,
            Name = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Name)?.Value,
            Claims = authenticateResult.Principal.Claims.ToList()
        };

        //authenticate grand user
        return await _externalAuthenticationService.Authenticate(authenticationParameters,
            _contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore,
            _contextAccessor.WorkContext.WorkingLanguage, returnUrl);
    }

    public IActionResult GoogleSignInFailed(string error_message)
    {
        //handle exception and display message to user
        var model = new FailedModel {
            ErrorMessage = error_message
        };
        return View(model);
    }

    #endregion
}