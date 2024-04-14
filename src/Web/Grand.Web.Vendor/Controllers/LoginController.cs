using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Models.Common;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Grand.Web.Vendor.Controllers;

[Area(Constants.AreaVendor)]
public class LoginController : BaseController
{
    private readonly IGrandAuthenticationService _authenticationService;
    private readonly CaptchaSettings _captchaSettings;
    private readonly ICustomerManagerService _customerManagerService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;
    private readonly IMediator _mediator;
    private readonly IMessageProviderService _messageProviderService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public LoginController(
        CustomerSettings customerSettings,
        CaptchaSettings captchaSettings,
        ITranslationService translationService,
        ICustomerManagerService customerManagerService,
        ICustomerService customerService,
        IGrandAuthenticationService authenticationService,
        IMessageProviderService messageProviderService,
        IWorkContext workContext,
        IMediator mediator)
    {
        _customerSettings = customerSettings;
        _captchaSettings = captchaSettings;
        _translationService = translationService;
        _customerManagerService = customerManagerService;
        _customerService = customerService;
        _authenticationService = authenticationService;
        _messageProviderService = messageProviderService;
        _workContext = workContext;
        _mediator = mediator;
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
                    HttpContext.Session.SetString("VendorRequiresTwoFactor", userName);
                    return RedirectToRoute("TwoFactorAuthorization");
                }
            }
        }

        //If we got this far, something failed, redisplay form
        model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
        model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage;

        return View(model);
    }

    protected async Task<IActionResult> SignInAction(Customer customer, bool createPersistent)
    {
        //sign in new customer
        await _authenticationService.SignIn(customer, createPersistent);

        //raise event       
        await _mediator.Publish(new CustomerLoggedInEvent(customer));

        return RedirectToRoute("VendorIndex", new RouteValueDictionary());
    }

    public async Task<IActionResult> TwoFactorAuthorization(
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("VendorLogin");

        var username = HttpContext.Session.GetString("VendorRequiresTwoFactor");
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute("VendorLogin");

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(username)
            : await _customerService.GetCustomerByEmail(username);
        if (customer == null)
            return RedirectToRoute("VendorLogin");

        if (!customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
            return RedirectToRoute("VendorLogin");

        if (_customerSettings.TwoFactorAuthenticationType != TwoFactorAuthenticationType.AppVerification)
        {
            await twoFactorAuthenticationService.GenerateCodeSetup("", customer, _workContext.WorkingLanguage,
                _customerSettings.TwoFactorAuthenticationType);
            if (_customerSettings.TwoFactorAuthenticationType == TwoFactorAuthenticationType.EmailVerification)
                await _messageProviderService.SendCustomerEmailTokenValidationMessage(customer,
                    _workContext.CurrentStore, _workContext.WorkingLanguage.Id);
        }

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> TwoFactorAuthorization(string token,
        [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
    {
        if (!_customerSettings.TwoFactorAuthenticationEnabled)
            return RedirectToRoute("VendorLogin");

        var username = HttpContext.Session.GetString("VendorRequiresTwoFactor");
        if (string.IsNullOrEmpty(username))
            return RedirectToRoute("HomePage");

        var customer = _customerSettings.UsernamesEnabled
            ? await _customerService.GetCustomerByUsername(username)
            : await _customerService.GetCustomerByEmail(username);
        if (customer == null)
            return RedirectToRoute("VendorLogin");

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
                HttpContext.Session.Remove("VendorRequiresTwoFactor");

                //sign in
                return await SignInAction(customer, false);
            }

            ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
        }

        await _mediator.Publish(new CustomerLoginFailedEvent(customer));
        return View();
    }
}