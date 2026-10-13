using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
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

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerProviderOnlyTests
{
    private static readonly ExternalRegistrationIdentity Pending =
        new("ExternalAuth.OpenIdConnect", "sub-1", "john@example.com", true, "John", "Doe");

    private Mock<IExternalSignInPathProvider> _provider;
    private Mock<IExternalAuthenticationService> _externalAuth;
    private Mock<IExternalRegistrationContext> _external;
    private OidcLoginConfig _providerOnly;
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
        work.Setup(w => w.CurrentCustomer).Returns(new Customer { Id = "g1" });
        work.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "l1" });
        var store = new Mock<IStoreContext>();
        store.Setup(s => s.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(store.Object);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetRegister>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterModel());
        var groups = new Mock<IGroupService>();
        groups.Setup(g => g.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);

        _controller = new AccountController(new Mock<IGrandAuthenticationService>().Object,
            new Mock<ITranslationService>().Object, context.Object, new Mock<ICustomerService>().Object,
            groups.Object, new Mock<ICustomerManagerService>().Object, new Mock<ICountryService>().Object,
            mediator.Object, new Mock<IMessageProviderService>().Object, new CaptchaSettings(),
            new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard }, new CustomerConfig());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, new Mock<ITempDataProvider>().Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://localhost/registerresult/1");
        url.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns((string u) =>
            !string.IsNullOrEmpty(u) && u.StartsWith('/') && !u.StartsWith("//"));
        _controller.Url = url.Object;
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
    public void PasswordRecovery_Mixed_ShowsForm()
    {
        var result = _controller.PasswordRecovery(new OidcLoginConfig());
        Assert.IsInstanceOfType<ViewResult>(result);
    }
}
