using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.Controllers;
using Grand.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

public abstract class BaseLoginController : BaseController
{
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

    public IActionResult Index()
    {
        var model = new LoginModel {
            UsernamesEnabled = _customerSettings.UsernamesEnabled,
            DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage
        };
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public virtual async Task<IActionResult> Index(LoginModel model)
    {
        if (ModelState.IsValid)
        {
            var loginResult =
                await _customerManagerService.LoginCustomer(
                    _customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password);
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
                        HttpContext.Session.SetString("RequiresTwoFactor", userName!);
                        return RedirectToRoute("TwoFactorAuthorization");
                    }
            }
        }

        //If we got this far, something failed, redisplay form
        model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage;

        return View(model);
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

        var username = HttpContext.Session.GetString("AdminRequiresTwoFactor");
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

        var username = HttpContext.Session.GetString("AdminRequiresTwoFactor");
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute("HomePage");

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
                HttpContext.Session.Remove("AdminRequiresTwoFactor");

                //sign in
                return await SignInAction(customer, false);
            }

            ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
        }

        await _mediator.Publish(new CustomerLoginFailedEvent(customer));
        return View();
    }
}