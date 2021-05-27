using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Events;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Customers.Queries.Models;
using Grand.Business.Customers.Utilities;
using Grand.Business.Messages.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Captcha;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Extensions;
using Grand.Web.Commands.Models.Customers;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Controllers
{
    public partial class AccountController : BasePublicController
    {
        #region Fields

        private readonly IGrandAuthenticationService _authenticationService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IUserFieldService _userFieldService;
        private readonly ICustomerManagerService _customerManagerService;
        private readonly ICountryService _countryService;
        private readonly IMediator _mediator;
        private readonly IMessageProviderService _messageProviderService;
        private readonly CustomerSettings _customerSettings;
        private readonly CaptchaSettings _captchaSettings;

        #endregion

        #region Ctor

        public AccountController(
            IGrandAuthenticationService authenticationService,
            ITranslationService translationService,
            IWorkContext workContext,
            ICustomerService customerService,
            IGroupService groupService,
            IUserFieldService userFieldService,
            ICustomerManagerService customerManagerService,
            ICountryService countryService,
            IMediator mediator,
            IMessageProviderService messageProviderService,
            CaptchaSettings captchaSettings,
            CustomerSettings customerSettings)
        {
            _authenticationService = authenticationService;
            _translationService = translationService;
            _workContext = workContext;
            _customerService = customerService;
            _groupService = groupService;
            _userFieldService = userFieldService;
            _customerManagerService = customerManagerService;
            _customerSettings = customerSettings;
            _countryService = countryService;
            _messageProviderService = messageProviderService;
            _captchaSettings = captchaSettings;
            _mediator = mediator;
        }

        #endregion

        #region Login / logout

        //available even when navigation is not allowed
        [PublicStore(true)]
        [ClosedStore(true)]
        public virtual IActionResult Login(bool? checkoutAsGuest)
        {
            var model = new LoginModel();
            model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
            model.CheckoutAsGuest = checkoutAsGuest.GetValueOrDefault();
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage;
            return View(model);
        }

        [HttpPost]
        //available even when navigation is not allowed
        [PublicStore(true)]
        [ClosedStore(true)]
        [ValidateCaptcha]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> Login(LoginModel model, string returnUrl, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (ModelState.IsValid)
            {
                if (_customerSettings.UsernamesEnabled && model.Username != null)
                {
                    model.Username = model.Username.Trim();
                }
                var loginResult = await _customerManagerService.LoginCustomer(_customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password);
                switch (loginResult)
                {
                    case CustomerLoginResults.Successful:
                        {
                            var customer = _customerSettings.UsernamesEnabled ? await _customerService.GetCustomerByUsername(model.Username) : await _customerService.GetCustomerByEmail(model.Email);
                            //sign in
                            return await SignInAction(customer, model.RememberMe, returnUrl);
                        }
                    case CustomerLoginResults.RequiresTwoFactor:
                        {
                            var userName = _customerSettings.UsernamesEnabled ? model.Username : model.Email;
                            HttpContext.Session.SetString("RequiresTwoFactor", userName);
                            return RedirectToRoute("TwoFactorAuthorization");
                        }

                    case CustomerLoginResults.CustomerNotExist:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.CustomerNotExist"));
                        break;
                    case CustomerLoginResults.Deleted:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.Deleted"));
                        break;
                    case CustomerLoginResults.NotActive:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.NotActive"));
                        break;
                    case CustomerLoginResults.NotRegistered:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.NotRegistered"));
                        break;
                    case CustomerLoginResults.LockedOut:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.LockedOut"));
                        break;
                    case CustomerLoginResults.WrongPassword:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials"));
                        break;
                    default:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials"));
                        break;
                }
            }

            //If we got this far, something failed, redisplay form
            model.UsernamesEnabled = _customerSettings.UsernamesEnabled;
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage;

            return View(model);
        }

        public async Task<IActionResult> TwoFactorAuthorization()
        {
            if (!_customerSettings.TwoFactorAuthenticationEnabled)
                return RedirectToRoute("Login");

            var username = HttpContext.Session.GetString("RequiresTwoFactor");
            if (string.IsNullOrEmpty(username))
                return RedirectToRoute("HomePage");

            var customer = _customerSettings.UsernamesEnabled ? await _customerService.GetCustomerByUsername(username) : await _customerService.GetCustomerByEmail(username);
            if (customer == null)
                return RedirectToRoute("HomePage");

            if (!customer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
                return RedirectToRoute("HomePage");

            if (_customerSettings.TwoFactorAuthenticationType != TwoFactorAuthenticationType.AppVerification)
            {
                await _mediator.Send(new GetTwoFactorAuthentication()
                {
                    Customer = customer,
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore,
                });
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TwoFactorAuthorization(string token,
            [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
        {
            if (!_customerSettings.TwoFactorAuthenticationEnabled)
                return RedirectToRoute("Login");

            var username = HttpContext.Session.GetString("RequiresTwoFactor");
            if (string.IsNullOrEmpty(username))
                return RedirectToRoute("HomePage");

            var customer = _customerSettings.UsernamesEnabled ? await _customerService.GetCustomerByUsername(username) : await _customerService.GetCustomerByEmail(username);
            if (customer == null)
                return RedirectToRoute("Login");

            if (string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
            }
            else
            {
                var secretKey = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.TwoFactorSecretKey);
                if (await twoFactorAuthenticationService.AuthenticateTwoFactor(secretKey, token, customer, _customerSettings.TwoFactorAuthenticationType))
                {
                    //remove session
                    HttpContext.Session.Remove("RequiresTwoFactor");

                    //sign in
                    return await SignInAction(customer);
                }
                ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
            }

            return View();
        }

        protected async Task<IActionResult> SignInAction(Customer customer, bool createPersistentCookie = false, string returnUrl = null)
        {
            //raise event       
            await _mediator.Publish(new CustomerLoggedInEvent(customer));

            //sign in new customer
            await _authenticationService.SignIn(customer, createPersistentCookie);

            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
                return RedirectToRoute("HomePage");

            return Redirect(returnUrl);
        }

        //available even when a store is closed
        [ClosedStore(true)]
        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> Logout([FromServices] StoreInformationSettings storeInformationSettings)
        {
            if (_workContext.OriginalCustomerIfImpersonated != null)
            {
                //logout impersonated customer
                await _userFieldService.SaveField<int?>(_workContext.OriginalCustomerIfImpersonated,
                    SystemCustomerFieldNames.ImpersonatedCustomerId, null);

                //redirect back to customer details page (admin area)
                return RedirectToAction("Edit", "Customer", new { id = _workContext.CurrentCustomer.Id, area = "Admin" });

            }

            //raise event       
            await _mediator.Publish(new CustomerLoggedOutEvent(_workContext.CurrentCustomer));

            //standard logout 
            await _authenticationService.SignOut();

            //Cookie
            if (storeInformationSettings.DisplayCookieInformation)
            {
                TempData["Grand.IgnoreCookieInformation"] = true;
            }
            return RedirectToRoute("HomePage");
        }

        #endregion

        #region Password recovery

        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual IActionResult PasswordRecovery()
        {
            var model = new PasswordRecoveryModel();
            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnPasswordRecoveryPage;
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [ValidateCaptcha]
        [PublicStore(true)]
        public virtual async Task<IActionResult> PasswordRecovery(PasswordRecoveryModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnPasswordRecoveryPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (ModelState.IsValid)
            {
                var customer = await _customerService.GetCustomerByEmail(model.Email);
                if (customer != null && customer.Active && !customer.Deleted)
                {
                    await _mediator.Send(new PasswordRecoverySendCommand() { Customer = customer, Store = _workContext.CurrentStore, Language = _workContext.WorkingLanguage, Model = model });

                    model.Result = _translationService.GetResource("Account.PasswordRecovery.EmailHasBeenSent");
                    model.Send = true;
                }
                else
                {
                    model.Result = _translationService.GetResource("Account.PasswordRecovery.EmailNotFound");
                }

                return View(model);
            }
            return View(model);
        }

        [PublicStore(true)]
        public virtual async Task<IActionResult> PasswordRecoveryConfirm(string token, string email)
        {
            var customer = await _customerService.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToRoute("HomePage");

            var model = await _mediator.Send(new GetPasswordRecoveryConfirm() { Customer = customer, Token = token });

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> PasswordRecoveryConfirm(string token, string email, PasswordRecoveryConfirmModel model)
        {
            var customer = await _customerService.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToRoute("HomePage");

            //validate token
            if (!customer.IsPasswordRecoveryTokenValid(token))
            {
                model.DisablePasswordChanging = true;
                model.Result = _translationService.GetResource("Account.PasswordRecovery.WrongToken");
            }

            //validate token expiration date
            if (customer.IsPasswordRecoveryLinkExpired(_customerSettings))
            {
                model.DisablePasswordChanging = true;
                model.Result = _translationService.GetResource("Account.PasswordRecovery.LinkExpired");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var response = await _customerManagerService.ChangePassword(new ChangePasswordRequest(email,
                    false, _customerSettings.DefaultPasswordFormat, model.NewPassword));
                if (response.Success)
                {
                    await _userFieldService.SaveField(customer, SystemCustomerFieldNames.PasswordRecoveryToken, "");

                    model.DisablePasswordChanging = true;
                    model.Result = _translationService.GetResource("Account.PasswordRecovery.PasswordHasBeenChanged");
                }
                else
                {
                    model.Result = response.Errors.FirstOrDefault();
                }

                return View(model);
            }
            return View(model);
        }

        #endregion

        #region Register

        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> Register()
        {
            //check whether registration is allowed
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });

            //check if customer is registered.
            if (await _groupService.IsRegistered(_workContext.CurrentCustomer))
            {
                return RedirectToRoute("HomePage");
            }

            var model = await _mediator.Send(new GetRegister()
            {
                Customer = _workContext.CurrentCustomer,
                ExcludeProperties = false,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });

            return View(model);
        }

        [HttpPost]
        [ValidateCaptcha]
        [AutoValidateAntiforgeryToken]
        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> Register(RegisterModel model, string returnUrl, bool captchaValid, IFormCollection form,
           [FromServices] ICustomerAttributeParser customerAttributeParser)
        {
            //check whether registration is allowed
            if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.Disabled });

            //check if customer is registered. 
            if (await _groupService.IsRegistered(_workContext.CurrentCustomer))
            {
                return RedirectToRoute("HomePage");
            }

            //custom customer attributes
            var customerAttributes = await _mediator.Send(new GetParseCustomAttributes() { Form = form });
            var customerAttributeWarnings = await customerAttributeParser.GetAttributeWarnings(customerAttributes);
            foreach (var error in customerAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnRegistrationPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_translationService));
            }

            if (ModelState.IsValid && ModelState.ErrorCount == 0)
            {
                if (_customerSettings.UsernamesEnabled && model.Username != null)
                {
                    model.Username = model.Username.Trim();
                }

                bool isApproved = _customerSettings.UserRegistrationType == UserRegistrationType.Standard;
                var registrationRequest = new RegistrationRequest(_workContext.CurrentCustomer, model.Email,
                    _customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password,
                    _customerSettings.DefaultPasswordFormat, _workContext.CurrentStore.Id, isApproved);
                var registrationResult = await _customerManagerService.RegisterCustomer(registrationRequest);
                if (registrationResult.Success)
                {
                    await _mediator.Send(new CustomerRegisteredCommand()
                    {
                        Customer = _workContext.CurrentCustomer,
                        CustomerAttributes = customerAttributes,
                        Form = form,
                        Model = model,
                        Store = _workContext.CurrentStore
                    });

                    //login customer now
                    if (isApproved)
                        await _authenticationService.SignIn(_workContext.CurrentCustomer, true);

                    //raise event       
                    await _mediator.Publish(new CustomerRegisteredEvent(_workContext.CurrentCustomer));

                    switch (_customerSettings.UserRegistrationType)
                    {
                        case UserRegistrationType.EmailValidation:
                            {
                                //email validation message
                                await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.AccountActivationToken, Guid.NewGuid().ToString());
                                await _messageProviderService.SendCustomerEmailValidationMessage(_workContext.CurrentCustomer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

                                //result
                                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.EmailValidation });
                            }
                        case UserRegistrationType.AdminApproval:
                            {
                                return RedirectToRoute("RegisterResult", new { resultId = (int)UserRegistrationType.AdminApproval });
                            }
                        case UserRegistrationType.Standard:
                            {
                                //send customer welcome message
                                await _messageProviderService.SendCustomerWelcomeMessage(_workContext.CurrentCustomer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

                                var redirectUrl = Url.RouteUrl("RegisterResult", new { resultId = (int)UserRegistrationType.Standard }, HttpContext.Request.Scheme);
                                if (!String.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                                {
                                    redirectUrl = CommonExtensions.ModifyQueryString(redirectUrl, "returnurl", returnUrl);
                                }
                                return Redirect(redirectUrl);
                            }
                        default:
                            {
                                return RedirectToRoute("HomePage");
                            }
                    }
                }

                //errors
                foreach (var error in registrationResult.Errors)
                    ModelState.AddModelError("", error);
            }

            //If we got this far, something failed, redisplay form
            model = await _mediator.Send(new GetRegister()
            {
                Customer = _workContext.CurrentCustomer,
                ExcludeProperties = true,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                Model = model,
                OverrideCustomCustomerAttributes = customerAttributes
            });

            return View(model);
        }
        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual IActionResult RegisterResult(int resultId)
        {
            var resultText = "";
            switch ((UserRegistrationType)resultId)
            {
                case UserRegistrationType.Disabled:
                    resultText = _translationService.GetResource("Account.Register.Result.Disabled");
                    break;
                case UserRegistrationType.Standard:
                    resultText = _translationService.GetResource("Account.Register.Result.Standard");
                    break;
                case UserRegistrationType.AdminApproval:
                    resultText = _translationService.GetResource("Account.Register.Result.AdminApproval");
                    break;
                case UserRegistrationType.EmailValidation:
                    resultText = _translationService.GetResource("Account.Register.Result.EmailValidation");
                    break;
                default:
                    break;
            }
            var model = new RegisterResultModel
            {
                Result = resultText
            };
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            var usernameAvailable = false;
            var statusText = _translationService.GetResource("Account.CheckUsernameAvailability.NotAvailable");

            if (_customerSettings.UsernamesEnabled && !String.IsNullOrWhiteSpace(username))
            {
                if (_workContext.CurrentCustomer != null &&
                    _workContext.CurrentCustomer.Username != null &&
                    _workContext.CurrentCustomer.Username.Equals(username, StringComparison.OrdinalIgnoreCase))
                {
                    statusText = _translationService.GetResource("Account.CheckUsernameAvailability.CurrentUsername");
                }
                else
                {
                    var customer = await _customerService.GetCustomerByUsername(username);
                    if (customer == null)
                    {
                        statusText = _translationService.GetResource("Account.CheckUsernameAvailability.Available");
                        usernameAvailable = true;
                    }
                }
            }

            return Json(new { Available = usernameAvailable, Text = statusText });
        }

        //available even when navigation is not allowed
        [PublicStore(true)]
        public virtual async Task<IActionResult> AccountActivation(string token, string email)
        {
            var customer = await _customerService.GetCustomerByEmail(email);
            if (customer == null)
                return RedirectToRoute("HomePage");

            var cToken = await customer.GetUserField<string>(_userFieldService, SystemCustomerFieldNames.AccountActivationToken);
            if (String.IsNullOrEmpty(cToken))
                return RedirectToRoute("HomePage");

            if (!cToken.Equals(token, StringComparison.OrdinalIgnoreCase))
                return RedirectToRoute("HomePage");

            //activate user account
            customer.Active = true;
            customer.StoreId = _workContext.CurrentStore.Id;
            await _customerService.UpdateActive(customer);
            await _userFieldService.SaveField(customer, SystemCustomerFieldNames.AccountActivationToken, "");

            //send welcome message
            await _messageProviderService.SendCustomerWelcomeMessage(customer, _workContext.CurrentStore, _workContext.WorkingLanguage.Id);

            var model = new AccountActivationModel();
            model.Result = _translationService.GetResource("Account.AccountActivation.Activated");
            return View(model);
        }

        #endregion

        #region My account / Info

        public virtual async Task<IActionResult> Info()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var model = await _mediator.Send(new GetInfo()
            {
                Customer = _workContext.CurrentCustomer,
                ExcludeProperties = false,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
            });
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> Info(CustomerInfoModel model, IFormCollection form,
            [FromServices] ICustomerAttributeParser customerAttributeParser)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            //custom customer attributes
            var customerAttributes = await _mediator.Send(new GetParseCustomAttributes() { Form = form, CustomerCustomAttribute = _workContext.CurrentCustomer.Attributes.ToList() });
            var customerAttributeWarnings = await customerAttributeParser.GetAttributeWarnings(customerAttributes);

            foreach (var error in customerAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            try
            {
                if (ModelState.IsValid && ModelState.ErrorCount == 0)
                {
                    await _mediator.Send(new UpdateCustomerInfoCommand()
                    {
                        Customer = _workContext.CurrentCustomer,
                        CustomerAttributes = customerAttributes,
                        Form = form,
                        Model = model,
                        OriginalCustomerIfImpersonated = _workContext.OriginalCustomerIfImpersonated,
                        Store = _workContext.CurrentStore
                    });
                    return RedirectToRoute("CustomerInfo");
                }
            }
            catch (Exception exc)
            {
                ModelState.AddModelError("", exc.Message);
            }

            //If we got this far, something failed, redisplay form
            model = await _mediator.Send(new GetInfo()
            {
                Model = model,
                Customer = _workContext.CurrentCustomer,
                ExcludeProperties = true,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                OverrideCustomCustomerAttributes = customerAttributes
            });

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> RemoveExternalAssociation(string id, [FromServices] IExternalAuthenticationService openAuthenticationService)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            //ensure it's our record
            var ear = (await openAuthenticationService.GetExternalIdentifiers(_workContext.CurrentCustomer))
                .FirstOrDefault(x => x.Id == id);

            if (ear == null)
            {
                return Json(new
                {
                    redirect = Url.Action("Info"),
                });
            }
            await openAuthenticationService.DeleteExternalAuthentication(ear);

            return Json(new
            {
                redirect = Url.Action("Info"),
            });
        }


        public virtual async Task<IActionResult> Export([FromServices] IExportManager exportManager)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.AllowUsersToExportData)
                return Challenge();

            var customer = _workContext.CurrentCustomer;
            byte[] bytes = await exportManager.ExportCustomerToXlsx(customer, _workContext.CurrentStore.Id);
            return File(bytes, "text/xls", "PersonalInfo.xlsx");

        }
        #endregion

        #region My account / Addresses

        public virtual async Task<IActionResult> Addresses()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var model = await _mediator.Send(new GetAddressList()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> AddressDelete(string addressId)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var customer = _workContext.CurrentCustomer;

            //find address (ensure that it belongs to the current customer)
            var address = customer.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address != null)
            {
                customer.RemoveAddress(address);
                await _customerService.DeleteAddress(address, customer.Id);
            }

            return Json(new
            {
                redirect = Url.RouteUrl("CustomerAddresses"),
            });

        }

        public virtual async Task<IActionResult> AddressAdd()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var countries = await _countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            var model = new CustomerAddressEditModel
            {
                Address = await _mediator.Send(new GetAddressModel()
                {
                    Language = _workContext.WorkingLanguage,
                    Store = _workContext.CurrentStore,
                    Model = null,
                    Address = null,
                    ExcludeProperties = false,
                    LoadCountries = () => countries
                })
            };

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> AddressAdd(CustomerAddressEditModel model, IFormCollection form,
            [FromServices] IAddressAttributeParser addressAttributeParser)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var customer = _workContext.CurrentCustomer;

            //custom address attributes
            var customAttributes = await _mediator.Send(new GetParseCustomAddressAttributes() { Form = form });
            var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid && ModelState.ErrorCount == 0)
            {
                var address = model.Address.ToEntity();
                address.Attributes = customAttributes;
                address.CreatedOnUtc = DateTime.UtcNow;
                customer.Addresses.Add(address);

                await _customerService.InsertAddress(address, customer.Id);

                return RedirectToRoute("CustomerAddresses");
            }
            var countries = await _countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            //If we got this far, something failed, redisplay form
            model.Address = await _mediator.Send(new GetAddressModel()
            {
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                Model = model.Address,
                Address = null,
                ExcludeProperties = true,
                LoadCountries = () => countries
            });

            return View(model);
        }

        public virtual async Task<IActionResult> AddressEdit(string addressId)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var customer = _workContext.CurrentCustomer;
            //find address (ensure that it belongs to the current customer)
            var address = customer.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                //address is not found
                return RedirectToRoute("CustomerAddresses");

            var countries = await _countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            var model = new CustomerAddressEditModel();
            model.Address = await _mediator.Send(new GetAddressModel()
            {
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                Model = model.Address,
                Address = address,
                ExcludeProperties = false,
                LoadCountries = () => countries
            });

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> AddressEdit(CustomerAddressEditModel model, string addressId, IFormCollection form,
            [FromServices] IAddressAttributeParser addressAttributeParser)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var customer = _workContext.CurrentCustomer;
            //find address (ensure that it belongs to the current customer)
            var address = customer.Addresses.FirstOrDefault(a => a.Id == addressId);
            if (address == null)
                //address is not found
                return RedirectToRoute("CustomerAddresses");

            //custom address attributes
            var customAttributes = await _mediator.Send(new GetParseCustomAddressAttributes() { Form = form });
            var customAttributeWarnings = await addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            if (ModelState.IsValid && ModelState.ErrorCount == 0)
            {
                address = model.Address.ToEntity(address);
                address.Attributes = customAttributes;
                await _customerService.UpdateAddress(address, customer.Id);

                if (customer.BillingAddress?.Id == address.Id)
                    await _customerService.UpdateBillingAddress(address, customer.Id);
                if (customer.ShippingAddress?.Id == address.Id)
                    await _customerService.UpdateShippingAddress(address, customer.Id);

                return RedirectToRoute("CustomerAddresses");
            }
            var countries = await _countryService.GetAllCountries(_workContext.WorkingLanguage.Id, _workContext.CurrentStore.Id);
            //If we got this far, something failed, redisplay form
            model.Address = await _mediator.Send(new GetAddressModel()
            {
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
                Model = model.Address,
                Address = address,
                ExcludeProperties = true,
                LoadCountries = () => countries
            });

            return View(model);
        }

        #endregion

        #region My account / Downloadable products

        public virtual async Task<IActionResult> DownloadableProducts()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideDownloadableProductsTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetDownloadableProducts() { Customer = _workContext.CurrentCustomer, Store = _workContext.CurrentStore, Language = _workContext.WorkingLanguage });
            return View(model);
        }

        public virtual async Task<IActionResult> UserAgreement(Guid orderItemId)
        {
            var model = await _mediator.Send(new GetUserAgreement() { OrderItemId = orderItemId }); ;
            if (model == null)
                return RedirectToRoute("HomePage");

            return View(model);
        }

        #endregion

        #region My account / Change password

        public virtual async Task<IActionResult> ChangePassword()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var model = new ChangePasswordModel();

            var passwordIsExpired = await _mediator.Send(new GetPasswordIsExpiredQuery() { Customer = _workContext.CurrentCustomer });
            if (passwordIsExpired)
                ModelState.AddModelError(string.Empty, _translationService.GetResource("Account.ChangePassword.PasswordIsExpired"));

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            var customer = _workContext.CurrentCustomer;

            if (ModelState.IsValid)
            {
                var changePasswordRequest = new ChangePasswordRequest(customer.Email,
                    true, _customerSettings.DefaultPasswordFormat, model.NewPassword, model.OldPassword);
                var changePasswordResult = await _customerManagerService.ChangePassword(changePasswordRequest);
                if (changePasswordResult.Success)
                {
                    //sign in
                    await _authenticationService.SignIn(customer, true);

                    model.Result = _translationService.GetResource("Account.ChangePassword.Success");
                    return View(model);
                }

                //errors
                foreach (var error in changePasswordResult.Errors)
                    ModelState.AddModelError("", error);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region My account / Delete account

        public virtual async Task<IActionResult> DeleteAccount()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.AllowUsersToDeleteAccount)
                return RedirectToRoute("CustomerInfo");

            var model = new DeleteAccountModel();

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> DeleteAccount(DeleteAccountModel model)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.AllowUsersToDeleteAccount)
                return RedirectToRoute("CustomerInfo");

            if (ModelState.IsValid)
            {
                var loginResult = await _customerManagerService.LoginCustomer(_customerSettings.UsernamesEnabled ? _workContext.CurrentCustomer.Username : _workContext.CurrentCustomer.Email, model.Password);

                switch (loginResult)
                {
                    case CustomerLoginResults.Successful:
                        {
                            //delete account 
                            await _mediator.Send(new DeleteAccountCommand() { Customer = _workContext.CurrentCustomer, Store = _workContext.CurrentStore });

                            //standard logout 
                            await _authenticationService.SignOut();

                            return RedirectToRoute("HomePage");
                        }
                    case CustomerLoginResults.CustomerNotExist:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.CustomerNotExist"));
                        break;
                    case CustomerLoginResults.Deleted:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.Deleted"));
                        break;
                    case CustomerLoginResults.NotActive:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.NotActive"));
                        break;
                    case CustomerLoginResults.NotRegistered:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.NotRegistered"));
                        break;
                    case CustomerLoginResults.LockedOut:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials.LockedOut"));
                        break;
                    case CustomerLoginResults.WrongPassword:
                    default:
                        ModelState.AddModelError("", _translationService.GetResource("Account.Login.WrongCredentials"));
                        break;
                }
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        #endregion

        #region My account / Auctions

        public virtual async Task<IActionResult> Auctions()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideAuctionsTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetAuctions() { Customer = _workContext.CurrentCustomer, Language = _workContext.WorkingLanguage });

            return View(model);
        }

        #endregion

        #region My account / Notes

        public virtual async Task<IActionResult> Notes()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideNotesTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetNotes() { Customer = _workContext.CurrentCustomer });

            return View(model);
        }

        #endregion

        #region My account / Documents

        public virtual async Task<IActionResult> Documents(DocumentPagingModel command)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideDocumentsTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetDocuments()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Command = command
            });

            return View(model);
        }

        #endregion

        #region My account / Reviews

        public virtual async Task<IActionResult> Reviews()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideReviewsTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetReviews() { Customer = _workContext.CurrentCustomer, Language = _workContext.WorkingLanguage });

            return View(model);
        }

        #endregion

        #region My account / TwoFactorAuth

        public async Task<IActionResult> EnableTwoFactorAuthenticator()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.TwoFactorAuthenticationEnabled)
                return RedirectToRoute("CustomerInfo");

            if (_workContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetTwoFactorAuthentication()
            {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
            });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EnableTwoFactorAuthenticator(CustomerInfoModel.TwoFactorAuthenticationModel model,
            [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.TwoFactorAuthenticationEnabled)
                return RedirectToRoute("CustomerInfo");

            if (_workContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
                return RedirectToRoute("CustomerInfo");

            if (string.IsNullOrEmpty(model.Code))
            {
                ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
            }
            else
            {
                if (await twoFactorAuthenticationService.AuthenticateTwoFactor(model.SecretKey, model.Code, _workContext.CurrentCustomer, _customerSettings.TwoFactorAuthenticationType))
                {
                    await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.TwoFactorEnabled, true);
                    await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.TwoFactorSecretKey, model.SecretKey);

                    Success(_translationService.GetResource("Account.TwoFactorAuth.Enabled"));

                    return RedirectToRoute("CustomerInfo");
                }
                ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
            }

            return View(model);
        }


        public async Task<IActionResult> DisableTwoFactorAuthenticator()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.TwoFactorAuthenticationEnabled)
                return RedirectToRoute("CustomerInfo");

            if (!_workContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetTwoFactorAuthentication() {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore,
            });
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> DisableTwoFactorAuthenticator(CustomerInfoModel.TwoFactorAuthenticationModel model,
            [FromServices] ITwoFactorAuthenticationService twoFactorAuthenticationService)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!_customerSettings.TwoFactorAuthenticationEnabled)
                return RedirectToRoute("CustomerInfo");

            if (!_workContext.CurrentCustomer.GetUserFieldFromEntity<bool>(SystemCustomerFieldNames.TwoFactorEnabled))
                return RedirectToRoute("CustomerInfo");

            if (string.IsNullOrEmpty(model.Code))
            {
                ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.SecurityCodeIsRequired"));
            }
            else
            {
                if (await twoFactorAuthenticationService.AuthenticateTwoFactor(model.SecretKey, model.Code, _workContext.CurrentCustomer, _customerSettings.TwoFactorAuthenticationType))
                {
                    await _userFieldService.SaveField(_workContext.CurrentCustomer, SystemCustomerFieldNames.TwoFactorEnabled, false);
                    await _userFieldService.SaveField<string>(_workContext.CurrentCustomer, SystemCustomerFieldNames.TwoFactorSecretKey, null);

                    Success(_translationService.GetResource("Account.TwoFactorAuth.Disabled"));

                    return RedirectToRoute("CustomerInfo");
                }
                ModelState.AddModelError("", _translationService.GetResource("Account.TwoFactorAuth.WrongSecurityCode"));
            }

            return View(model);
        }



        #endregion

        #region My account / Courses

        public virtual async Task<IActionResult> Courses()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideCoursesTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetCourses() { Customer = _workContext.CurrentCustomer, Store = _workContext.CurrentStore });

            return View(model);
        }

        #endregion

        #region My account / Sub accounts

        public virtual async Task<IActionResult> SubAccounts()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!await _groupService.IsOwner(_workContext.CurrentCustomer))
                return Challenge();

            if (_customerSettings.HideSubAccountsTab)
                return RedirectToRoute("CustomerInfo");

            var model = await _mediator.Send(new GetSubAccounts() { Customer = _workContext.CurrentCustomer });

            return View(model);
        }

        public virtual async Task<IActionResult> SubAccountAdd()
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!await _groupService.IsOwner(_workContext.CurrentCustomer))
                return Challenge();

            var model = new SubAccountModel()
            {
                Active = true,
            };
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> SubAccountAdd(SubAccountModel model, IFormCollection form)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!await _groupService.IsOwner(_workContext.CurrentCustomer))
                return Challenge();

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new SubAccountAddCommand()
                {
                    Customer = _workContext.CurrentCustomer,
                    Model = model,
                    Form = form,
                    Store = _workContext.CurrentStore
                });

                if (result.Success)
                {
                    return RedirectToRoute("CustomerSubAccounts");
                }

                //errors
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        public virtual async Task<IActionResult> SubAccountEdit(string id)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!await _groupService.IsOwner(_workContext.CurrentCustomer))
                return Challenge();

            var model = await _mediator.Send(new GetSubAccount() { CustomerId = id, CurrentCustomer = _workContext.CurrentCustomer });

            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> SubAccountEdit(SubAccountModel model, IFormCollection form)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!await _groupService.IsOwner(_workContext.CurrentCustomer))
                return Challenge();

            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new SubAccountEditCommand()
                {
                    CurrentCustomer = _workContext.CurrentCustomer,
                    Model = model,
                    Form = form,
                    Store = _workContext.CurrentStore
                });

                if (result.success)
                {
                    return RedirectToRoute("CustomerSubAccounts");
                }

                //errors
                ModelState.AddModelError("", result.error);
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [AutoValidateAntiforgeryToken]
        public virtual async Task<IActionResult> SubAccountDelete(string id)
        {
            if (!await _groupService.IsRegistered(_workContext.CurrentCustomer))
                return Challenge();

            if (!await _groupService.IsOwner(_workContext.CurrentCustomer))
                return Challenge();

            //find address (ensure that it belongs to the current customer)
            if (ModelState.IsValid)
            {
                var result = await _mediator.Send(new SubAccountDeleteCommand()
                {
                    CurrentCustomer = _workContext.CurrentCustomer,
                    CustomerId = id,
                });

                if (result.success)
                {
                    return Json(new
                    {
                        redirect = Url.RouteUrl("CustomerSubAccounts"),
                        success = true,
                    });
                }

                //errors
                ModelState.AddModelError("", result.error);
            }
            return Json(new
            {
                redirect = Url.RouteUrl("CustomerSubAccounts"),
                success = false,
                error = string.Join("; ", ModelState.Values
                                        .SelectMany(x => x.Errors)
                                        .Select(x => x.ErrorMessage))
            });
        }


        #endregion

    }
}
