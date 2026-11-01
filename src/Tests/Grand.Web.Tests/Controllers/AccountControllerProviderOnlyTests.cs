using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Common.Security.ExternalRegistration;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerProviderOnlyTests
{
    private static readonly ExternalRegistrationIdentity Pending =
        new("ExternalAuth.OpenIdConnect", "sub-1", "john@example.com", true, "John", "Doe");

    private Mock<IExternalSignInPathProvider> _provider;
    private Mock<IExternalAuthenticationService> _externalAuth;
    private Mock<IExternalRegistrationContext> _external;
    private Mock<ICustomerManagerService> _customerManager;
    private Mock<ICustomerService> _customers;
    private OidcLoginConfig _providerOnly;
    private Mock<IGroupService> _groups;
    private Customer _currentCustomer;
    private CustomerSettings _customerSettings;
    private AccountController _controller;

    [TestInitialize]
    public void Init()
    {
        _provider = new Mock<IExternalSignInPathProvider>();
        _provider.Setup(p => p.SystemName).Returns("ExternalAuth.OpenIdConnect");
        _externalAuth = new Mock<IExternalAuthenticationService>();
        _externalAuth.Setup(x => x.LoadActiveAuthenticationProviders(It.IsAny<Customer>(), It.IsAny<Grand.Domain.Stores.Store>()))
            .Returns(new List<IExternalAuthenticationProvider> { _provider.Object });
        _external = new Mock<IExternalRegistrationContext>();
        _providerOnly = new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly };

        var work = new Mock<IWorkContext>();
        _currentCustomer = new Customer { Id = "g1" };
        work.Setup(w => w.CurrentCustomer).Returns(_currentCustomer);
        work.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "l1" });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetRegister>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterModel());
        _groups = new Mock<IGroupService>();
        _groups.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);

        _customerManager = new Mock<ICustomerManagerService>();
        _customers = new Mock<ICustomerService>();
        _controller = new AccountController(new Mock<IGrandAuthenticationService>().Object,
            new Mock<IRequestTranslationService>().Object, context.Object, _customers.Object,
            _groups.Object, _customerManager.Object, new Mock<ICountryService>().Object,
            mediator.Object, new Mock<ICustomerMessageProviderService>().Object, new CaptchaSettings(),
            _customerSettings = new CustomerSettings {
                UserRegistrationType = UserRegistrationType.Standard, TwoFactorAuthenticationEnabled = true,
                AllowUsersToDeleteAccount = true
            }, new CustomerConfig());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, new Mock<ITempDataProvider>().Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://localhost/registerresult/1");
        url.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns((string u) =>
            !string.IsNullOrEmpty(u) && u.StartsWith('/') && !u.StartsWith("//"));
        _controller.Url = url.Object;
    }

    [TestMethod]
    public async Task ChangePassword_ChangesThePasswordOfTheSignedInAccount()
    {
        await _controller.ChangePassword(new ChangePasswordModel { OldPassword = "old", NewPassword = "new" },
            new OidcLoginConfig());

        _customerManager.Verify(m => m.ChangePassword(_currentCustomer, It.IsAny<PasswordFormat>(), "new"), Times.Once);
        _customerManager.Verify(m => m.ChangePassword(It.IsAny<ChangePasswordRequest>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_ProviderOnly_RedirectsToProvider()
    {
        _provider.Setup(p => p.GetSignInPath("/cart")).ReturnsAsync("/oidc/login?returnUrl=%2Fcart");
        var result = await _controller.Login(null, "/cart", _providerOnly, _externalAuth.Object);
        Assert.AreEqual("/oidc/login?returnUrl=%2Fcart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_ProviderOnly_PathBaseIsPrepended()
    {
        _controller.HttpContext.Request.PathBase = "/shop";
        _provider.Setup(p => p.GetSignInPath("/cart")).ReturnsAsync("/oidc/login?returnUrl=%2Fcart");
        var result = await _controller.Login(null, "/cart", _providerOnly, _externalAuth.Object);
        Assert.AreEqual("/shop/oidc/login?returnUrl=%2Fcart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_ExternalReturnUrl_NotForwarded()
    {
        _provider.Setup(p => p.GetSignInPath(It.IsAny<string>())).ReturnsAsync("/oidc/login");
        var result = await _controller.Login(null, "https://evil.example", _providerOnly, _externalAuth.Object);
        Assert.AreEqual("/oidc/login", ((RedirectResult)result).Url);
        _provider.Verify(p => p.GetSignInPath(null), Times.Once);
        _provider.Verify(p => p.GetSignInPath("https://evil.example"), Times.Never);
    }

    [TestMethod]
    public async Task Login_NoSignInPath_ShowsNotice()
    {
        _provider.Setup(p => p.GetSignInPath(It.IsAny<string>())).ReturnsAsync((string)null);
        var result = await _controller.Login(null, "/cart", _providerOnly, _externalAuth.Object);
        var model = (LoginModel)((ViewResult)result).Model;
        Assert.IsTrue(model.ExternalSignInUnavailable);
    }

    [TestMethod]
    public async Task Login_ProviderOnly_SignedIn_ShowsSignOutNotice_ProviderNotAsked()
    {
        _groups.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);
        _provider.Setup(p => p.GetSignInPath(It.IsAny<string>())).ReturnsAsync("/oidc/login");

        var result = await _controller.Login(null, "/cart", _providerOnly, _externalAuth.Object);

        var model = (LoginModel)((ViewResult)result).Model;
        Assert.IsTrue(model.SignedInWithoutAccess);
        _provider.Verify(p => p.GetSignInPath(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_Mixed_SignedIn_ShowsForm()
    {
        _groups.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(true);

        var result = await _controller.Login(null, "/cart", new OidcLoginConfig(), _externalAuth.Object);

        var model = (LoginModel)((ViewResult)result).Model;
        Assert.IsFalse(model.SignedInWithoutAccess);
        Assert.IsFalse(model.ExternalSignInUnavailable);
    }

    [TestMethod]
    public async Task Login_ProviderOnly_FirstProviderWithoutPath_UsesNextProvider()
    {
        var first = new Mock<IExternalSignInPathProvider>();
        first.Setup(p => p.GetSignInPath(It.IsAny<string>())).ReturnsAsync((string)null);
        _provider.Setup(p => p.GetSignInPath("/cart")).ReturnsAsync("/oidc/login?returnUrl=%2Fcart");
        _externalAuth.Setup(x => x.LoadActiveAuthenticationProviders(It.IsAny<Customer>(), It.IsAny<Grand.Domain.Stores.Store>()))
            .Returns(new List<IExternalAuthenticationProvider> { first.Object, _provider.Object });

        var result = await _controller.Login(null, "/cart", _providerOnly, _externalAuth.Object);

        Assert.AreEqual("/oidc/login?returnUrl=%2Fcart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_Mixed_ShowsForm()
    {
        var result = await _controller.Login(null, "/cart", new OidcLoginConfig(), _externalAuth.Object);
        var model = (LoginModel)((ViewResult)result).Model;
        Assert.IsFalse(model.ExternalSignInUnavailable);
        _provider.Verify(p => p.GetSignInPath(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Register_ProviderOnly_NoPending_RedirectsToProvider()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        _provider.Setup(p => p.GetSignInPath("/cart")).ReturnsAsync("/oidc/login?returnUrl=%2Fcart");
        var result = (await _controller.Register(_external.Object, _externalAuth.Object, _providerOnly, "/cart")).Result;
        Assert.AreEqual("/oidc/login?returnUrl=%2Fcart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Register_AutoProvision_RegistrationDisabled_RedirectsToProvider()
    {
        _customerSettings.UserRegistrationType = UserRegistrationType.Disabled;
        _providerOnly.AutoProvision = true;
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        _provider.Setup(p => p.GetSignInPath("/cart")).ReturnsAsync("/oidc/login?returnUrl=%2Fcart");
        var result = (await _controller.Register(_external.Object, _externalAuth.Object, _providerOnly, "/cart")).Result;
        Assert.AreEqual("/oidc/login?returnUrl=%2Fcart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Register_AutoProvision_Pending_RedirectsToProvider_NoForm()
    {
        _providerOnly.AutoProvision = true;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _provider.Setup(p => p.GetSignInPath(It.IsAny<string>())).ReturnsAsync("/oidc/login");
        var result = (await _controller.Register(_external.Object, _externalAuth.Object, _providerOnly)).Result;
        Assert.AreEqual("/oidc/login", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Register_ProviderOnly_RegistrationDisabled_WithoutAutoProvision_RegisterResult()
    {
        _customerSettings.UserRegistrationType = UserRegistrationType.Disabled;
        var result = (await _controller.Register(_external.Object, _externalAuth.Object, _providerOnly)).Result;
        Assert.AreEqual("RegisterResult", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task Register_ProviderOnly_NoPending_NoSignInPath_ShowsNotice()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        _provider.Setup(p => p.GetSignInPath(It.IsAny<string>())).ReturnsAsync((string)null);
        var view = (ViewResult)(await _controller.Register(_external.Object, _externalAuth.Object, _providerOnly)).Result;
        Assert.AreEqual("Login", view.ViewName);
        Assert.IsTrue(((LoginModel)view.Model).ExternalSignInUnavailable);
    }

    [TestMethod]
    public async Task Register_ProviderOnly_Pending_ShowsForm()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        var view = (ViewResult)(await _controller.Register(_external.Object, _externalAuth.Object, _providerOnly)).Result;
        var model = (RegisterModel)view.Model;
        Assert.IsTrue(model.ExternalRegistration);
        Assert.AreEqual("john@example.com", model.Email);
        _provider.Verify(p => p.GetSignInPath(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RegisterPost_ProviderOnly_UnverifiedPending_RedirectsToLogin()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = false });
        var result = await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, _providerOnly);
        Assert.AreEqual("Login", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task RegisterPost_ProviderOnly_NoPending_RedirectsToLogin()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        var result = await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, _providerOnly);
        Assert.AreEqual("Login", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task RegisterExternalCancel_ProviderOnly_ClearsPendingAndGoesHome()
    {
        var result = await _controller.RegisterExternalCancel(_external.Object, _providerOnly);

        Assert.AreEqual("HomePage", ((RedirectToRouteResult)result).RouteName);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task RegisterExternalCancel_Mixed_BackToRegister()
    {
        var result = await _controller.RegisterExternalCancel(_external.Object, new OidcLoginConfig());

        Assert.AreEqual("Register", ((RedirectToRouteResult)result).RouteName);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public void PasswordRecovery_ProviderOnly_RedirectsToLogin()
    {
        var result = _controller.PasswordRecovery(_providerOnly);
        Assert.AreEqual("Login", ((RedirectToRouteResult)result).RouteName);
    }

    [TestMethod]
    public async Task PasswordRecoveryPost_ProviderOnly_RedirectsToLogin()
    {
        var result = await _controller.PasswordRecovery(new PasswordRecoveryModel { Email = "a@b.c" }, _providerOnly);
        Assert.AreEqual("Login", ((RedirectToRouteResult)result.Result).RouteName);
    }

    [TestMethod]
    public async Task PasswordRecoveryConfirm_ProviderOnly_RedirectsToLogin()
    {
        var get = await _controller.PasswordRecoveryConfirm("t", "a@b.c", _providerOnly);
        Assert.AreEqual("Login", ((RedirectToRouteResult)get.Result).RouteName);
        var post = await _controller.PasswordRecoveryConfirm(new PasswordRecoveryConfirmModel(), _providerOnly);
        Assert.AreEqual("Login", ((RedirectToRouteResult)post).RouteName);
    }

    [TestMethod]
    public async Task ChangePassword_ProviderOnly_Redirects()
    {
        var get = await _controller.ChangePassword(_providerOnly);
        Assert.AreEqual("CustomerInfo", ((RedirectToRouteResult)get).RouteName);
        var post = await _controller.ChangePassword(
            new ChangePasswordModel { OldPassword = "a", NewPassword = "b", ConfirmNewPassword = "b" }, _providerOnly);
        Assert.AreEqual("CustomerInfo", ((RedirectToRouteResult)post).RouteName);
        _customerManager.Verify(m => m.ChangePassword(It.IsAny<ChangePasswordRequest>(), It.IsAny<string>()), Times.Never);
        _customerManager.Verify(m => m.ChangePassword(It.IsAny<Customer>(), It.IsAny<PasswordFormat>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task ChangePassword_Mixed_ChangesPassword()
    {
        _customers.Setup(c => c.GetCustomerById(It.IsAny<string>())).ReturnsAsync(new Customer { Id = "g1" });
        var post = await _controller.ChangePassword(
            new ChangePasswordModel { OldPassword = "a", NewPassword = "b", ConfirmNewPassword = "b" },
            new OidcLoginConfig());
        Assert.IsInstanceOfType<ViewResult>(post);
        _customerManager.Verify(m => m.ChangePassword(_currentCustomer, It.IsAny<PasswordFormat>(), "b"), Times.Once);
    }

    [TestMethod]
    public async Task EnableTwoFactor_ProviderOnly_Redirects()
    {
        var model = new CustomerInfoModel.TwoFactorAuthenticationModel { Code = "123456", SecretKey = "k" };
        var get = await _controller.EnableTwoFactorAuthenticator(_providerOnly);
        Assert.AreEqual("CustomerInfo", ((RedirectToRouteResult)get).RouteName);
        var post = await _controller.EnableTwoFactorAuthenticator(model,
            new Mock<ITwoFactorAuthenticationService>().Object, _providerOnly);
        Assert.AreEqual("CustomerInfo", ((RedirectToRouteResult)post).RouteName);
        _customers.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        _customers.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task EnableTwoFactor_Mixed_ShowsForm()
    {
        var get = await _controller.EnableTwoFactorAuthenticator(new OidcLoginConfig());
        Assert.IsInstanceOfType<ViewResult>(get);
    }

    private void TwoFactorEnabled() =>
        _currentCustomer.UserFields.Add(new UserField
            { Key = SystemCustomerFieldNames.TwoFactorEnabled, Value = "True", StoreId = "" });

    [TestMethod]
    public async Task DisableTwoFactor_ProviderOnly_Redirects()
    {
        //two-factor is on: only the provider-only guard can send the customer away
        TwoFactorEnabled();
        var get = await _controller.DisableTwoFactorAuthenticator(_providerOnly);
        Assert.AreEqual("CustomerInfo", ((RedirectToRouteResult)get).RouteName);
        var post = await _controller.DisableTwoFactorAuthenticator(
            new CustomerInfoModel.TwoFactorAuthorizationModel { Code = "123456" },
            new Mock<ITwoFactorAuthenticationService>().Object, _providerOnly);
        Assert.AreEqual("CustomerInfo", ((RedirectToRouteResult)post).RouteName);
        _customers.Verify(c => c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task DisableTwoFactor_Mixed_ShowsForm()
    {
        TwoFactorEnabled();
        var get = await _controller.DisableTwoFactorAuthenticator(new OidcLoginConfig());
        Assert.IsInstanceOfType<ViewResult>(get);
    }

    private void ExistingLinks(params string[] providers)
    {
        _externalAuth.Setup(x => x.GetExternalIdentifiers(It.IsAny<Customer>())).ReturnsAsync(
            (IList<ExternalAuthentication>)providers
                .Select(p => new ExternalAuthentication { Id = p, ProviderSystemName = p }).ToList());
    }

    [TestMethod]
    public async Task RemoveExternalAssociation_ProviderOnly_OidcKept()
    {
        ExistingLinks("ExternalAuth.OpenIdConnect", "ExternalAuth.Google");
        await _controller.RemoveExternalAssociation("ExternalAuth.OpenIdConnect", _externalAuth.Object, _providerOnly);
        _externalAuth.Verify(x => x.DeleteExternalAuthentication(It.IsAny<ExternalAuthentication>()), Times.Never);

        await _controller.RemoveExternalAssociation("ExternalAuth.Google", _externalAuth.Object, _providerOnly);
        _externalAuth.Verify(x => x.DeleteExternalAuthentication(
            It.Is<ExternalAuthentication>(e => e.Id == "ExternalAuth.Google")), Times.Once);
    }

    [TestMethod]
    public async Task RemoveExternalAssociation_Mixed_OidcRemoved()
    {
        ExistingLinks("ExternalAuth.OpenIdConnect");
        await _controller.RemoveExternalAssociation("ExternalAuth.OpenIdConnect", _externalAuth.Object,
            new OidcLoginConfig());
        _externalAuth.Verify(x => x.DeleteExternalAuthentication(
            It.Is<ExternalAuthentication>(e => e.Id == "ExternalAuth.OpenIdConnect")), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAccount_ProviderOnly_AsksNoPassword_AlsoOnRedisplay()
    {
        var get = (DeleteAccountModel)((ViewResult)_controller.DeleteAccount(_providerOnly)).Model;
        Assert.IsFalse(get.PasswordRequired);

        _controller.ModelState.AddModelError("", "failed");
        var post = (DeleteAccountModel)((ViewResult)await _controller.DeleteAccount(new DeleteAccountModel(), _providerOnly)).Model;
        Assert.IsFalse(post.PasswordRequired);
    }

    [TestMethod]
    public void DeleteAccount_Mixed_AsksPassword()
    {
        var get = (DeleteAccountModel)((ViewResult)_controller.DeleteAccount(new OidcLoginConfig())).Model;
        Assert.IsTrue(get.PasswordRequired);
    }

    [TestMethod]
    public void PasswordRecovery_Mixed_ShowsForm()
    {
        var result = _controller.PasswordRecovery(new OidcLoginConfig());
        Assert.IsInstanceOfType<ViewResult>(result);
    }
}
