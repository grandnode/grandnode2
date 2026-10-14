using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.AdminShared.Extensions;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Controllers;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseLoginController : BaseController
{
    //the account waiting for its second factor on a panel; never the storefront's key, whose page signs in on the storefront
    private const string TwoFactorSessionKey = "AdminRequiresTwoFactor";

    private readonly IGrandAuthenticationService _authenticationService;
    private readonly CaptchaSettings _captchaSettings;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;
    private readonly IMediator _mediator;
    private readonly IMessageProviderService _messageProviderService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public BaseLoginController(
        CustomerSettings customerSettings,
        CaptchaSettings captchaSettings,
        ITranslationService translationService,
        ICustomerManagerService customerManagerService,
        ICustomerService customerService,
        IGrandAuthenticationService authenticationService,
        IMessageProviderService messageProviderService,
        IContextAccessor contextAccessor,
        IMediator mediator)
    {
        _customerSettings = customerSettings;
        _captchaSettings = captchaSettings;
        _translationService = translationService;
        _customerManagerService = customerManagerService;
        _customerService = customerService;
        _authenticationService = authenticationService;
        _messageProviderService = messageProviderService;
        _contextAccessor = contextAccessor;
        _mediator = mediator;
    }
    protected virtual string GetCurrentArea()
    {
        return RouteData.Values["area"]?.ToString() ?? "Admin";
    }

    public async Task<IActionResult> Index([FromServices] IExternalAuthenticationService externalAuthenticationService,
        [FromServices] OidcLoginConfig oidcLoginConfig, [FromServices] IGroupService groupService,
        string password = null)
    {
        var model = new LoginModel {
            UsernamesEnabled = _customerSettings.UsernamesEnabled,
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage
        };
        await PrepareExternalSignIn(model, externalAuthenticationService);
        if (!oidcLoginConfig.ProviderOnly)
        {
            model.PasswordFormShown = true;
            return View(model);
        }

        //the identity provider is the only sign-in: go there, or say it cannot be reached (never redirect back here);
        //only the Admin panel keeps the password form, for break-glass accounts, behind ?password=1
        var keepsPasswordForm = KeepsPasswordForm();
        //a signed-in account without access to the panel is sent here: the provider would sign it in again and return
        //to the panel (a loop), so offer the sign-out instead
        model.SignedInWithoutAccess = await groupService.IsRegistered(_contextAccessor.WorkContext.CurrentCustomer);
        if (model.ExternalSignIn.Count > 0 && !model.SignedInWithoutAccess &&
            !(keepsPasswordForm && PasswordRequested(password)))
            return Redirect(model.ExternalSignIn[0].Url);
        model.PasswordFormShown = keepsPasswordForm;
        model.ExternalSignInUnavailable = model.ExternalSignIn.Count == 0;
        return View(model);
    }

    //?password=1 (as documented) or ?password=true; a bool parameter would not bind "1"
    private static bool PasswordRequested(string password) =>
        password == "1" || string.Equals(password, "true", StringComparison.OrdinalIgnoreCase);

    private bool KeepsPasswordForm() => PanelSignInSurface.FromArea(GetCurrentArea()) == SignInSurface.AdminPanel;

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public virtual async Task<IActionResult> Index(LoginModel model,
        [FromServices] IExternalAuthenticationService externalAuthenticationService,
        [FromServices] OidcLoginConfig oidcLoginConfig)
    {
        if (ModelState.IsValid)
        {
            var loginResult =
                await _customerManagerService.LoginCustomer(
                    _customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password, "",
                    PanelSignInSurface.FromArea(GetCurrentArea()));
            switch (loginResult)
            {
                case CustomerLoginResults.Successful:
                    {
                        var customer = _customerSettings.UsernamesEnabled
                            ? await _customerService.GetCustomerByUsername(model.Username)
                            : await _customerService.GetCustomerByEmail(model.Email);
                        //sign in
                        return await SignInAction(customer, model.RememberMe);
                    }
                case CustomerLoginResults.RequiresTwoFactor:
                    {
                        var userName = _customerSettings.UsernamesEnabled ? model.Username : model.Email;
                        //the panel's own second step: the storefront's page would sign in on the storefront
                        HttpContext.Session.SetString(TwoFactorSessionKey, userName!);
                        return RedirectToAction(nameof(TwoFactorAuthorization), "Login", new { area = GetCurrentArea() });
                    }
            }
        }

        //If we got this far, something failed, redisplay form
        model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage;
        await PrepareExternalSignIn(model, externalAuthenticationService);
        //a failed break-glass attempt stays on the form; the other panels never show it in ProviderOnly
        model.PasswordFormShown = !oidcLoginConfig.ProviderOnly || KeepsPasswordForm();
        model.ExternalSignInUnavailable = oidcLoginConfig.ProviderOnly && model.ExternalSignIn.Count == 0;

        return View(model);
    }

    //the panel's home is where the provider sign-in returns; only providers made for the panels are offered
    private async Task PrepareExternalSignIn(LoginModel model, IExternalAuthenticationService externalAuthenticationService)
    {
        var returnUrl = Url.RouteUrl($"{GetCurrentArea()}Index");
        foreach (var provider in externalAuthenticationService.LoadActiveAuthenticationProviders()
                     .OfType<IExternalSignInPathProvider>())
        {
            var path = await provider.GetSignInPath(returnUrl);
            if (!string.IsNullOrEmpty(path))
                model.ExternalSignIn.Add(new LoginModel.ExternalSignInButton {
                    Name = provider.FriendlyName, Url = Request.PathBase + path
                });
        }
    }

    #region Password recovery

    public IActionResult PasswordRecovery()
    {
        var model = new PasswordRecoveryModel {
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnPasswordRecoveryPage
        };
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public virtual async Task<IActionResult> PasswordRecovery(PasswordRecoveryModel model,
        [FromServices] IPanelPasswordRecoveryService passwordRecoveryService)
    {
        if (ModelState.IsValid)
        {
            await passwordRecoveryService.SendRecoveryMessage(model.Email, GetCurrentArea());
            //the same answer whether or not the email belongs to an account of this panel
            model.Sent = true;
            model.Result = _translationService.GetResource("Account.PasswordRecovery.EmailHasBeenSent");
        }

        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnPasswordRecoveryPage;
        return View(model);
    }

    public async Task<IActionResult> PasswordRecoveryConfirm(string token, string email,
        [FromServices] IPanelPasswordRecoveryService passwordRecoveryService)
    {
        var model = new PasswordRecoveryConfirmModel { Email = email, Token = token };
        var error = await passwordRecoveryService.ValidateToken(email, token, GetCurrentArea());
        if (error != null)
        {
            model.DisablePasswordChanging = true;
            model.Result = error;
        }

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public virtual async Task<IActionResult> PasswordRecoveryConfirm(PasswordRecoveryConfirmModel model,
        [FromServices] IPanelPasswordRecoveryService passwordRecoveryService)
    {
        if (!ModelState.IsValid)
            return View(model);

        var error = await passwordRecoveryService.ResetPassword(model.Email, model.Token, GetCurrentArea(),
            model.NewPassword);
        if (error != null)
        {
            ModelState.AddModelError("", error);
            return View(model);
        }

        model.DisablePasswordChanging = true;
        model.PasswordChanged = true;
        model.Result = _translationService.GetResource("Account.PasswordRecovery.PasswordHasBeenChanged");
        return View(model);
    }

    #endregion

    protected async Task<IActionResult> SignInAction(Customer customer, bool createPersistent)
    {
        //sign in new customer
        await _authenticationService.SignIn(customer, createPersistent);

        //raise event       
        await _mediator.Publish(new CustomerLoggedInEvent(customer));

        return RedirectToRoute($"{GetCurrentArea()}Index");
    }

    public async Task<IActionResult> TwoFactorAuthorization(
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute($"{GetCurrentArea()}Login");

        var username = HttpContext.Session.GetString(TwoFactorSessionKey);
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute($"{GetCurrentArea()}Login");

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(username)
            : await _customerService.GetCustomerByEmail(username);
        if (customer == null)
            return RedirectToRoute($"{GetCurrentArea()}Login");

        if (!customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute($"{GetCurrentArea()}Login");

        if (_customerSettings.TwoFactorAuthenticationType != TwoFactorAuthenticationType.AppVerification)
        {
            await twoFactorAuthenticationService.GenerateCodeSetup("", customer, _contextAccessor.WorkContext.WorkingLanguage,
                _customerSettings.TwoFactorAuthenticationType);
            if (_customerSettings.TwoFactorAuthenticationType == TwoFactorAuthenticationType.EmailVerification)
                await _messageProviderService.SendCustomerEmailTokenValidationMessage(customer,
                    _contextAccessor.StoreContext.CurrentStore, _contextAccessor.WorkContext.WorkingLanguage.Id);
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> TwoFactorAuthorization(string token,
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute($"{GetCurrentArea()}Login");

        var username = HttpContext.Session.GetString(TwoFactorSessionKey);
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute($"{GetCurrentArea()}Login");

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(username)
            : await _customerService.GetCustomerByEmail(username);
        if (customer == null)
            return RedirectToRoute($"{GetCurrentArea()}Login");

        if (string.IsNullOrEmpty(token))
        {
            ModelState.AddModelError("",
                _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
        }
        else
        {
            var secretKey = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorSecretKey);
            if (await twoFactorAuthenticationService.AuthenticateTwoFactor(secretKey, token, customer,
                    _customerSettings.TwoFactorAuthenticationType))
            {
                //remove session
                HttpContext.Session.Remove(TwoFactorSessionKey);

                //sign in
                return await SignInAction(customer, false);
            }

            ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
        }

        await _mediator.Publish(new CustomerLoginFailedEvent(customer));
        return View();
    }
}