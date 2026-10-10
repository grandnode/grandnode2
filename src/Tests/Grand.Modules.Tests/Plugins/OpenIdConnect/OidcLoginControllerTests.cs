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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Globalization;
using System.Security.Claims;

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
    private Mock<IAuthenticationService> _httpAuth;
    private Mock<ILogger<OidcLoginController>> _logger;
    private DateTimeOffset? _ticketIssued;
    private readonly List<UrlRouteContext> _routes = [];

    private const string MarkerKey = "Grand.OidcLogin.LinkSubject";

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _data = new();
        public bool IsAvailable => true;
        public string Id => "s";
        public IEnumerable<string> Keys => _data.Keys;
        public void Clear() => _data.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _data.Remove(key);
        public void Set(string key, byte[] value) => _data[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _data.TryGetValue(key, out value);
    }

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

        _httpAuth = new Mock<IAuthenticationService>();
        _ticketIssued = DateTimeOffset.UtcNow;
        _httpAuth.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(() => AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("test")),
                new AuthenticationProperties { IssuedUtc = _ticketIssued }, GrandCookieAuthenticationDefaults.AuthenticationScheme)));
        _logger = new Mock<ILogger<OidcLoginController>>();
        var http = new DefaultHttpContext {
            Session = new TestSession(),
            RequestServices = new ServiceCollection().AddSingleton(_httpAuth.Object).BuildServiceProvider()
        };
        _controller = new OidcLoginController(_external.Object, _resolver.Object, _auth.Object, _mediator.Object,
            _externalAuth.Object, _schemes.Object, _groups.Object, ctx.Object, _logger.Object) {
            ControllerContext = new ControllerContext { HttpContext = http }
        };
        var url = new Mock<IUrlHelper>();
        url.Setup(x => x.IsLocalUrl(It.IsAny<string>())).Returns<string>(u => !string.IsNullOrEmpty(u) && u.StartsWith('/') && !u.StartsWith("//"));
        url.Setup(x => x.RouteUrl(It.IsAny<UrlRouteContext>())).Returns<UrlRouteContext>(c => {
            _routes.Add(c);
            return "/" + c.RouteName + (c.Values == null ? "" : "?" + string.Join("&", c.Values.GetType().GetProperties()
                .Select(p => p.Name + "=" + p.GetValue(c.Values))));
        });
        _controller.Url = url.Object;
        _controller.TempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>().Object;
    }

    [TestMethod]
    public async Task Login_ExternalReturnUrl_Dropped()
    {
        var result = (ChallengeResult)await _controller.Login("https://evil.example/");
        Assert.AreEqual(OidcLoginDefaults.AuthenticationScheme, result.AuthenticationSchemes.Single());
        Assert.IsFalse(result.Properties!.RedirectUri!.Contains("evil"));
        Assert.AreEqual("/OidcLoginCallback?returnUrl=", result.Properties.RedirectUri);
    }

    [TestMethod]
    public async Task Login_NotConfigured_Failed()
    {
        _schemes.Setup(x => x.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme)).ReturnsAsync((AuthenticationScheme)null);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Login("/")).RouteName);
        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(
                "OpenID Connect login is installed but OidcResourceServer:Authority or OidcLogin:ClientId is empty")),
            It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
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
        Marker("sub-1");
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Assert.AreEqual("/account", ((RedirectResult)await _controller.LinkComplete("/account")).Url);
    }

    [TestMethod]
    public async Task LinkComplete_OtherEmail_NothingLinked()
    {
        _current = new Customer { Id = "c2", Email = "someone@else.com" };
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        Marker("sub-1");
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
        Marker("sub-1");
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.SubjectLinkedToOtherCustomer);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
    }

    //the link page was shown a minute ago unless a test says otherwise
    private void Marker(string subject, DateTimeOffset? shown = null) =>
        _controller.HttpContext.Session.SetString(MarkerKey,
            (shown ?? DateTimeOffset.UtcNow.AddMinutes(-1)).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) + ":" + subject);

    [TestMethod]
    public async Task LinkComplete_TicketIssuedBeforeLink_BackToLink_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1", DateTimeOffset.UtcNow);
        _ticketIssued = DateTimeOffset.UtcNow.AddMinutes(-5);
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_TicketWithoutIssueTime_BackToLink_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1");
        _ticketIssued = null;
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_TicketIssuedAfterLink_Links()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1", DateTimeOffset.UtcNow.AddMinutes(-2));
        _ticketIssued = DateTimeOffset.UtcNow.AddMinutes(-1);
        Assert.AreEqual("/account", ((RedirectResult)await _controller.LinkComplete("/account")).Url);
        _external.Verify(x => x.Complete(_current), Times.Once);
    }

    [TestMethod]
    public async Task Callback_SignIn_ExternalReturnUrl_GoesHome()
    {
        var customer = new Customer { Id = "c9" };
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        Assert.AreEqual("HomePage", ((RedirectToRouteResult)await _controller.Callback("https://evil.example/")).RouteName);
    }

    [TestMethod]
    public async Task LinkComplete_ExternalReturnUrl_GoesHome()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1");
        Assert.AreEqual("HomePage", ((RedirectToRouteResult)await _controller.LinkComplete("https://evil.example/")).RouteName);
    }

    [TestMethod]
    public async Task Link_RegisteredCustomer_SignedOutAndMarkerSet()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        var result = await _controller.Link("/");
        Assert.IsInstanceOfType<ViewResult>(result);
        _httpAuth.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties>()), Times.Once);
        _httpAuth.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme,
            It.IsAny<AuthenticationProperties>()), Times.Never);
        _auth.Verify(x => x.SignOut(), Times.Never);
        var marker = _controller.HttpContext.Session.GetString(MarkerKey)!.Split(':', 2);
        Assert.AreEqual("sub-1", marker[1]);
        Assert.IsTrue(Math.Abs(long.Parse(marker[0], CultureInfo.InvariantCulture) - DateTimeOffset.UtcNow.ToUnixTimeSeconds()) <= 5);
    }

    [TestMethod]
    public async Task LinkComplete_NoMarker_BackToLink_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_OtherSubjectMarker_BackToLink_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        Marker("sub-other");
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_Success_RemovesMarker()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current)).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1");
        await _controller.LinkComplete("/");
        Assert.IsNull(_controller.HttpContext.Session.GetString(MarkerKey));
    }

    [TestMethod]
    public async Task LinkComplete_OtherProviderPending_ClearsAndFails()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { ProviderSystemName = "Other" });
        Marker("sub-1");
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.Once);
    }
}
