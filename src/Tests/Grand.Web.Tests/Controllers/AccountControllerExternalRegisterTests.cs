using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
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
using Grand.Web.Common.Localization;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class AccountControllerExternalRegisterTests
{
    private static readonly ExternalRegistrationIdentity Pending =
        new("ExternalAuth.OpenIdConnect", "sub-1", "john@example.com", true, "John", "Doe");

    private Mock<ICustomerManagerService> _manager;
    private Mock<IMessageProviderService> _messages;
    private Mock<IExternalRegistrationContext> _external;
    private Mock<IExternalAuthenticationService> _externalAuth;
    private CustomerSettings _settings;
    private AccountController _controller;
    private readonly Customer _guest = new() { Id = "g1" };

    [TestInitialize]
    public void Init()
    {
        _manager = new Mock<ICustomerManagerService>();
        _messages = new Mock<IMessageProviderService>();
        _external = new Mock<IExternalRegistrationContext>();
        _externalAuth = new Mock<IExternalAuthenticationService>();
        _settings = new CustomerSettings { UserRegistrationType = UserRegistrationType.Standard };

        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(_guest);
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
            new Mock<IRequestTranslationService>().Object, context.Object, new Mock<ICustomerService>().Object,
            groups.Object, _manager.Object, new Mock<ICountryService>().Object, mediator.Object, _messages.Object,
            new CaptchaSettings(), _settings, new CustomerConfig());
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, new Mock<ITempDataProvider>().Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns("https://localhost/registerresult/1");
        url.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns((string u) =>
            !string.IsNullOrEmpty(u) && u.StartsWith('/') && !u.StartsWith("//"));
        _controller.Url = url.Object;
    }

    [TestMethod]
    public async Task Get_Pending_PrefillsAndMarksExternal()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        var view = (ViewResult)(await _controller.Register(_external.Object, _externalAuth.Object, new OidcLoginConfig())).Result;
        var model = (RegisterModel)view.Model;
        Assert.AreEqual("john@example.com", model.Email);
        Assert.AreEqual("John", model.FirstName);
        Assert.IsTrue(model.ExternalRegistration);
        Assert.IsTrue(model.ExternalConnects);
    }

    [TestMethod]
    public async Task Post_Pending_UsesPendingEmail_RandomPassword_Links()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel { Email = "evil@b.c", ExternalRegistration = false }, null,
            _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.AreEqual("john@example.com", request.Email);
        Assert.IsFalse(string.IsNullOrEmpty(request.Password));
        _external.Verify(x => x.Complete(_guest, "s1"), Times.Once);
    }

    [TestMethod]
    [DataRow(UserRegistrationType.Standard, true, true)]
    [DataRow(UserRegistrationType.EmailValidation, true, true)]
    [DataRow(UserRegistrationType.Standard, false, false)]
    [DataRow(UserRegistrationType.AdminApproval, true, false)]
    public async Task Post_Pending_ApprovalFollowsPolicy(UserRegistrationType type, bool verified, bool approved)
    {
        _settings.UserRegistrationType = type;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = verified });
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.AreEqual(approved, request.IsApproved);
    }

    [TestMethod]
    public async Task Post_Pending_Unverified_SendsActivation()
    {
        _settings.UserRegistrationType = UserRegistrationType.Standard;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = false });
        var result = await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        _messages.Verify(x => x.SendCustomerEmailValidationMessage(_guest, It.IsAny<Grand.Domain.Stores.Store>(), "l1"), Times.Once);
        Assert.AreEqual((int)UserRegistrationType.EmailValidation, ((RedirectToRouteResult)result).RouteValues["resultId"]);
    }

    [TestMethod]
    public async Task Post_Pending_Unverified_TypedPassword_NotConnected()
    {
        //whoever holds an identity with an unconfirmed address must not keep the account once its owner activates it
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = false });
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel { Password = "Secret1!" }, null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.AreEqual("john@example.com", request.Email);
        Assert.AreEqual("Secret1!", request.Password);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task Get_Pending_Unverified_AsksForPassword()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { EmailVerified = false });
        var view = (ViewResult)(await _controller.Register(_external.Object, _externalAuth.Object, new OidcLoginConfig())).Result;
        var model = (RegisterModel)view.Model;
        Assert.IsTrue(model.ExternalRegistration);
        Assert.IsFalse(model.ExternalConnects);
    }

    [TestMethod]
    public async Task Post_Pending_LinkFails_WarnsCustomer()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_guest, "s1")).ReturnsAsync(OidcLinkResult.SubjectLinkedToOtherCustomer);
        await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.IsNotNull(_controller.TempData["grand.notifications.Warning"]);
    }

    [TestMethod]
    public async Task Post_NoPending_RegularPath_NoLink()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        RegistrationRequest request = null;
        _manager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>())).Callback<RegistrationRequest>(r => request = r);
        await _controller.Register(new RegisterModel { Email = "a@b.c", Password = "Secret1!" }, null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.AreEqual("Secret1!", request.Password);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    private Mock<IExternalSignInPathProvider> PathProvider(string path = "/oidc/login?returnUrl=%2Fregisterresult%2F1")
    {
        var provider = new Mock<IExternalSignInPathProvider>();
        provider.Setup(x => x.GetSignInPath(It.IsAny<string>())).ReturnsAsync(path);
        _externalAuth.Setup(x => x.LoadAuthenticationProviderBySystemName("ExternalAuth.OpenIdConnect")).Returns(provider.Object);
        return provider;
    }

    [TestMethod]
    public async Task Post_Pending_LinkedAndSignedIn_GoesThroughProviderSignIn()
    {
        _settings.UserRegistrationType = UserRegistrationType.Standard;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_guest, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        var provider = PathProvider();

        var result = (RedirectResult)await _controller.Register(new RegisterModel(), "/cart", _external.Object, _externalAuth.Object, new OidcLoginConfig());

        Assert.AreEqual("/oidc/login?returnUrl=%2Fregisterresult%2F1", result.Url);
        provider.Verify(x => x.GetSignInPath(It.Is<string>(u =>
            u.StartsWith("/") && u.Contains("/registerresult/1") && u.Contains("returnurl=%2Fcart"))), Times.Once);
    }

    [TestMethod]
    public async Task Post_Pending_NotApproved_NoProviderRedirect()
    {
        _settings.UserRegistrationType = UserRegistrationType.AdminApproval;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_guest, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        var provider = PathProvider();
        var result = await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.IsInstanceOfType(result, typeof(RedirectToRouteResult));
        provider.Verify(x => x.GetSignInPath(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Post_Pending_NotLinked_NoProviderRedirect()
    {
        _settings.UserRegistrationType = UserRegistrationType.Standard;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_guest, "s1")).ReturnsAsync(OidcLinkResult.SubjectLinkedToOtherCustomer);
        var provider = PathProvider();
        var result = (RedirectResult)await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.IsFalse(result.Url.Contains("/oidc/login"));
        provider.Verify(x => x.GetSignInPath(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Post_Pending_ProviderWithoutSignInPath_NoProviderRedirect()
    {
        _settings.UserRegistrationType = UserRegistrationType.Standard;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_guest, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        _externalAuth.Setup(x => x.LoadAuthenticationProviderBySystemName("ExternalAuth.OpenIdConnect"))
            .Returns(new Mock<IExternalAuthenticationProvider>().Object);
        var result = (RedirectResult)await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.AreEqual("https://localhost/registerresult/1", result.Url);
    }

    [TestMethod]
    public async Task Post_Pending_NullSignInPath_NoProviderRedirect()
    {
        _settings.UserRegistrationType = UserRegistrationType.Standard;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_guest, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        PathProvider(null);
        var result = (RedirectResult)await _controller.Register(new RegisterModel(), null, _external.Object, _externalAuth.Object, new OidcLoginConfig());
        Assert.AreEqual("https://localhost/registerresult/1", result.Url);
    }
}
