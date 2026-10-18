using Authentication.Facebook.Models;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Infrastructure;
using Grand.SharedKernel;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Authentication.Facebook.Controllers;

public class FacebookAuthenticationController : BasePluginController
{
    #region Ctor

    public FacebookAuthenticationController(
        IExternalAuthenticationService externalAuthenticationService,
        IExternalSignInService externalSignInService,
        IConfiguration configuration,
        IContextAccessor contextAccessor)
    {
        _externalAuthenticationService = externalAuthenticationService;
        _externalSignInService = externalSignInService;
        _configuration = configuration;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Fields

    private readonly IExternalAuthenticationService _externalAuthenticationService;
    private readonly IExternalSignInService _externalSignInService;
    private readonly IConfiguration _configuration;
    private readonly IContextAccessor _contextAccessor;

    #endregion

    #region Methods

    public IActionResult FacebookLogin(string returnUrl)
    {
        if (!_externalAuthenticationService.AuthenticationProviderIsAvailable(
                FacebookAuthenticationDefaults.ProviderSystemName, _contextAccessor.WorkContext.CurrentCustomer,
                _contextAccessor.StoreContext.CurrentStore))
            throw new GrandException("Facebook authentication module cannot be loaded");

        if (string.IsNullOrEmpty(_configuration["FacebookSettings:AppId"]) ||
            string.IsNullOrEmpty(_configuration["FacebookSettings:AppSecret"]))
            throw new GrandException("Facebook authentication module not configured");

        //configure login callback action
        var authenticationProperties = new AuthenticationProperties {
            RedirectUri = Url.Action("FacebookLoginCallback", "FacebookAuthentication", new { returnUrl })
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
            Identifier =
                authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.NameIdentifier)?.Value,
            Name = authenticateResult.Principal.FindFirst(claim => claim.Type == ClaimTypes.Name)?.Value,
            Claims = authenticateResult.Principal.Claims.ToList()
        };

        //authenticate Grand user
        return await _externalSignInService.Authenticate(authenticationParameters,
            _contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore,
            _contextAccessor.WorkContext.WorkingLanguage, returnUrl);
    }

    public IActionResult FacebookSignInFailed(string error_code, string error_message, string state)
    {
        //handle exception and display message to user
        var model = new FailedModel {
            ErrorCode = error_code,
            ErrorMessage = error_message
        };
        return View(model);
    }

    #endregion
}