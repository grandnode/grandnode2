using Authentication.OpenIdConnect;
using Authentication.OpenIdConnect.Controllers;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Common.Security.ExternalRegistration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcLoginControllerTests
{
    private static readonly ExternalRegistrationIdentity Pending =
        new(OidcLoginDefaults.ProviderSystemName, "sub-1", "john@example.com", true, "John", "Doe");

    private Mock<IExternalRegistrationContext> _external;
    private Mock<IOidcCustomerResolver> _resolver;
    private Mock<IGrandAuthenticationService> _auth;
    private Mock<IMediator> _mediator;
    private Mock<IExternalAuthenticationService> _externalAuth;
    private Mock<IAuthenticationSchemeProvider> _schemes;
    private Mock<IGroupService> _groups;
    private Customer _current;
    private OidcLoginController _controller;

    [TestInitialize]
    public void Init()
    {
        _external = new Mock<IExternalRegistrationContext>();
        _resolver = new Mock<IOidcCustomerResolver>();
        _auth = new Mock<IGrandAuthenticationService>();
        _mediator = new Mock<IMediator>();
        _externalAuth = new Mock<IExternalAuthenticationService>();
        _externalAuth.Setup(x => x.AuthenticationProviderIsAvailable(OidcLoginDefaults.ProviderSystemName)).Returns(true);
        _schemes = new Mock<IAuthenticationSchemeProvider>();
        _schemes.Setup(x => x.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme)).ReturnsAsync(
            new AuthenticationScheme(OidcLoginDefaults.AuthenticationScheme, null, typeof(IAuthenticationHandler)));
        _groups = new Mock<IGroupService>();
        _current = new Customer { Id = "c1", Email = "John@Example.com" };
        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(() => _current);
        var store = new Mock<IStoreContext>();
        store.Setup(x => x.CurrentStore).Returns(new Store { Id = "s1" });
        var ctx = new Mock<IContextAccessor>();
        ctx.Setup(x => x.WorkContext).Returns(work.Object);
        ctx.Setup(x => x.StoreContext).Returns(store.Object);

        _controller = new OidcLoginController(_external.Object, _resolver.Object, _auth.Object, _mediator.Object,
            _externalAuth.Object, _schemes.Object, _groups.Object, ctx.Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns<string>(u => !string.IsNullOrEmpty(u) && u.StartsWith('/') && !u.StartsWith("//"));
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns<UrlRouteContext>(c => "/" + c.RouteName);
        _controller.Url = url.Object;
    }

    [TestMethod]
    public async Task Login_ExternalReturnUrl_Dropped()
    {
        var result = (ChallengeResult)await _controller.Login("https://evil.example/");
        Assert.AreEqual(OidcLoginDefaults.AuthenticationScheme, result.AuthenticationSchemes.Single());
        Assert.IsFalse(result.Properties!.RedirectUri!.Contains("evil"));
    }

    [TestMethod]
    public async Task Login_NotConfigured_Failed()
    {
        _schemes.Setup(x => x.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme)).ReturnsAsync((AuthenticationScheme)null);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Login("/")).RouteName);
    }

    [TestMethod]
    public async Task Login_MethodInactive_Failed()
    {
        _externalAuth.Setup(x => x.AuthenticationProviderIsAvailable(It.IsAny<string>())).Returns(false);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Login("/")).RouteName);
    }

    [TestMethod]
    public async Task Callback_NoPending_Failed()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync((ExternalRegistrationIdentity)null);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Callback("/")).RouteName);
    }

    [TestMethod]
    public async Task Callback_SignIn_SignsInClearsRedirects()
    {
        var customer = new Customer { Id = "c9" };
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin("sub-1", "john@example.com", true, "s1")).ReturnsAsync(OidcLoginResult.SignIn(customer));
        var result = await _controller.Callback("/cart");
        _auth.Verify(x => x.SignIn(customer, false), Times.Once);
        _mediator.Verify(x => x.Publish(It.Is<CustomerLoggedInEvent>(e => e.Customer == customer), It.IsAny<CancellationToken>()), Times.Once);
        _external.Verify(x => x.Clear(), Times.Once);
        Assert.AreEqual("/cart", ((RedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Callback_RegistrationRequired_ToRegister_PendingKept()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.RegistrationRequired());
        Assert.AreEqual("Register", ((RedirectToRouteResult)await _controller.Callback("/")).RouteName);
        _external.Verify(x => x.Clear(), Times.Never);
    }

    [TestMethod]
    public async Task Callback_LinkRequired_ToLink()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.LinkRequired(new Customer()));
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.Callback("/")).RouteName);
    }

    [TestMethod]
    public async Task Callback_Refused_ClearsAndFailsWithReason()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.Refused(OidcLoginRefusal.AccountInactive));
        var result = (RedirectToRouteResult)await _controller.Callback("/");
        Assert.AreEqual("OidcLoginFailed", result.RouteName);
        Assert.AreEqual((int)OidcLoginRefusal.AccountInactive, result.RouteValues!["reason"]);
        _external.Verify(x => x.Clear(), Times.Once);
        _auth.Verify(x => x.SignIn(It.IsAny<Customer>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_NotSignedIn_BackToLink()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(false);
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
    }

    [TestMethod]
    public async Task LinkComplete_EmailMatches_Links()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Assert.AreEqual("/account", ((RedirectResult)await _controller.LinkComplete("/account")).Url);
    }

    [TestMethod]
    public async Task LinkComplete_OtherEmail_NothingLinked()
    {
        _current = new Customer { Id = "c2", Email = "someone@else.com" };
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/");
        Assert.AreEqual("OidcLoginFailed", result.RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task LinkComplete_SubjectTaken_Failed()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.SubjectLinkedToOtherCustomer);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
    }
}
