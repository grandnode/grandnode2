using Authentication.OpenIdConnect;
using Authentication.OpenIdConnect.Controllers;
using Authentication.OpenIdConnect.Infrastructure;
using Authentication.OpenIdConnect.Models;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Common;
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
    private Mock<IOidcSessionService> _sessions;
    private Dictionary<string, string> _externalItems;
    private Mock<IMediator> _mediator;
    private Mock<IExternalAuthenticationService> _externalAuth;
    private Mock<IAuthenticationSchemeProvider> _schemes;
    private Mock<IGroupService> _groups;
    private Customer _current;
    private OidcLoginController _controller;
    private Mock<IAuthenticationService> _httpAuth;
    private Mock<ILogger<OidcLoginController>> _logger;
    private Mock<ICustomerService> _customers;
    private OidcLoginConfig _loginConfig;
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
        _sessions = new Mock<IOidcSessionService>();
        _sessions.Setup(x => x.Start(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("session-1");
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
        _externalItems = new Dictionary<string, string> {
            [OidcLoginSessionData.RefreshTokenItem] = "rt-1", [OidcLoginSessionData.SidItem] = "sid-1"
        };
        _httpAuth.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme))
            .ReturnsAsync(() => AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("ext")),
                new AuthenticationProperties(_externalItems), GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme)));
        _logger = new Mock<ILogger<OidcLoginController>>();
        _customers = new Mock<ICustomerService>();
        _loginConfig = new OidcLoginConfig();
        var sync = new OidcProfileSynchronizer(_customers.Object, new CustomerSettings(), new CustomerConfig(), _loginConfig,
            new Mock<ILogger<OidcProfileSynchronizer>>().Object);
        var http = new DefaultHttpContext {
            Session = new TestSession(),
            RequestServices = new ServiceCollection().AddSingleton(_httpAuth.Object).BuildServiceProvider()
        };
        _controller = new OidcLoginController(_external.Object, _resolver.Object, sync, _loginConfig, _auth.Object, _sessions.Object, _mediator.Object,
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
                "or the application was not restarted after the plugin was installed")),
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
        _auth.Verify(x => x.SignIn(customer, false, "session-1"), Times.Once);
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
        _auth.Verify(x => x.SignIn(It.IsAny<Customer>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
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
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/account");
        Assert.AreEqual("OidcLoginLogin", result.RouteName);
        Assert.AreEqual("/account", result.RouteValues!["returnUrl"]);
    }

    //the password session of "Connect your account" is replaced at once: the provider session is still open, so the
    //browser comes straight back and the callback starts a session-backed cookie
    [TestMethod]
    public async Task LinkComplete_Linked_GoesThroughProviderSignIn()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        Marker("sub-1");
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/account");
        Assert.AreEqual("OidcLoginLogin", result.RouteName);
        Assert.AreEqual("/account", result.RouteValues!["returnUrl"]);
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
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task LinkComplete_SubjectTaken_FailedWithIdentityTaken()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        Marker("sub-1");
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.SubjectLinkedToOtherCustomer);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/");
        Assert.AreEqual("OidcLoginFailed", result.RouteName);
        Assert.AreEqual(true, result.RouteValues!["taken"]);
        Assert.IsFalse(result.RouteValues.ContainsKey("reason"));
    }

    [TestMethod]
    public async Task LinkComplete_CustomerLinkedToOtherSubject_FailedWithLinkedToOtherIdentity()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        Marker("sub-1");
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.CustomerLinkedToOtherSubject);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/");
        Assert.AreEqual((int)OidcLoginRefusal.LinkedToOtherIdentity, result.RouteValues!["reason"]);
    }

    [TestMethod]
    public void Failed_IdentityTaken_InModel()
    {
        var model = (FailedModel)((ViewResult)_controller.Failed(null, taken: true)).Model!;
        Assert.IsTrue(model.IdentityTaken);
    }

    [TestMethod]
    public async Task Callback_MethodInactive_ClearsAndFails_NothingResolved()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _externalAuth.Setup(x => x.AuthenticationProviderIsAvailable(It.IsAny<string>())).Returns(false);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Callback("/")).RouteName);
        _external.Verify(x => x.Clear(), Times.Once);
        _resolver.Verify(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Callback_OtherProviderPending_ClearsAndFails_NothingResolved()
    {
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending with { ProviderSystemName = "Other" });
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Callback("/")).RouteName);
        _external.Verify(x => x.Clear(), Times.Once);
        _resolver.Verify(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Link_MethodInactive_ClearsAndFails_StaysSignedIn()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _externalAuth.Setup(x => x.AuthenticationProviderIsAvailable(It.IsAny<string>())).Returns(false);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Link("/")).RouteName);
        _external.Verify(x => x.Clear(), Times.Once);
        _httpAuth.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_MethodInactive_ClearsAndFails_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        Marker("sub-1");
        _externalAuth.Setup(x => x.AuthenticationProviderIsAvailable(It.IsAny<string>())).Returns(false);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task Callback_SignIn_TwoFactorCustomer_SignsInWithoutStoreCode()
    {
        var customer = new Customer { Id = "c9", Email = "john@example.com" };
        customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.TwoFactorEnabled, Value = "True", StoreId = "" });
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        Assert.AreEqual("/cart", ((RedirectResult)await _controller.Callback("/cart")).Url);
        _auth.Verify(x => x.SignIn(customer, false, "session-1"), Times.Once);
    }

    [TestMethod]
    public async Task Callback_SignIn_OtherAccountSignedIn_ClearsAndFails_SessionKept()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(new Customer { Id = "other" }));
        var result = (RedirectToRouteResult)await _controller.Callback("/");
        Assert.AreEqual("OidcLoginFailed", result.RouteName);
        Assert.AreEqual(true, result.RouteValues!["signedIn"]);
        _external.Verify(x => x.Clear(), Times.Once);
        _auth.Verify(x => x.SignIn(It.IsAny<Customer>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        _mediator.Verify(x => x.Publish(It.IsAny<CustomerLoggedInEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //the same account signing in again (right after "Connect your account") replaces its cookie with a session-backed one
    [TestMethod]
    public async Task Callback_SameCustomerSignedIn_ReplacesCookieWithSession()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(_current));

        var result = (RedirectResult)await _controller.Callback("/account");

        Assert.AreEqual("/account", result.Url);
        _sessions.Verify(x => x.Start(_current, "sub-1", "sid-1", "rt-1", "s1"), Times.Once);
        _auth.Verify(x => x.SignIn(_current, false, "session-1"), Times.Once);
    }

    [TestMethod]
    public async Task Callback_RegistrationRequired_SignedIn_ClearsAndFails()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.RegistrationRequired());
        var result = (RedirectToRouteResult)await _controller.Callback("/");
        Assert.AreEqual("OidcLoginFailed", result.RouteName);
        Assert.AreEqual(true, result.RouteValues!["signedIn"]);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task Callback_LinkRequired_SignedIn_StillToLink()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.LinkRequired(new Customer { Id = "other" }));
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.Callback("/")).RouteName);
    }

    [TestMethod]
    public async Task LinkComplete_TicketIssuedInSameSecondAsLink_Links()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        var now = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Marker("sub-1", now);
        _ticketIssued = now.AddMilliseconds(800);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/account");
        Assert.AreEqual("OidcLoginLogin", result.RouteName);
        Assert.AreEqual("/account", result.RouteValues!["returnUrl"]);
    }

    [TestMethod]
    public void Failed_SignedIn_InModel()
    {
        var model = (FailedModel)((ViewResult)_controller.Failed(null, signedIn: true)).Model!;
        Assert.IsTrue(model.SignedInAsOtherAccount);
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
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1", DateTimeOffset.UtcNow);
        _ticketIssued = DateTimeOffset.UtcNow.AddMinutes(-5);
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_TicketWithoutIssueTime_BackToLink_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1");
        _ticketIssued = null;
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_TicketIssuedAfterLink_Links()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1", DateTimeOffset.UtcNow.AddMinutes(-2));
        _ticketIssued = DateTimeOffset.UtcNow.AddMinutes(-1);
        var result = (RedirectToRouteResult)await _controller.LinkComplete("/account");
        Assert.AreEqual("OidcLoginLogin", result.RouteName);
        Assert.AreEqual("/account", result.RouteValues!["returnUrl"]);
        _external.Verify(x => x.Complete(_current, "s1"), Times.Once);
    }

    [TestMethod]
    public async Task Callback_SignIn_StartsSessionWithTokensOfTheSignIn()
    {
        var customer = new Customer { Id = "c2" };
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin("sub-1", "john@example.com", true, "s1")).ReturnsAsync(OidcLoginResult.SignIn(customer));

        await _controller.Callback("/");

        _sessions.Verify(x => x.Start(customer, "sub-1", "sid-1", "rt-1", "s1"), Times.Once);
        _auth.Verify(x => x.SignIn(customer, false, "session-1"), Times.Once);
    }

    //a back-office account whose provider sent no refresh token (offline_access missing): no session that dies at once
    [TestMethod]
    public async Task Callback_SessionNotStarted_FailedPage_NotSignedIn()
    {
        var customer = new Customer { Id = "c2" };
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        _sessions.Setup(x => x.Start(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string)null);

        var result = (RedirectToRouteResult)await _controller.Callback("/");

        Assert.AreEqual("OidcLoginFailed", result.RouteName);
        Assert.AreEqual(true, result.RouteValues!["session"]);
        _auth.Verify(x => x.SignIn(It.IsAny<Customer>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.AtLeastOnce);
    }

    [TestMethod]
    public void Failed_SessionUnavailable_Model()
    {
        var model = (FailedModel)((ViewResult)_controller.Failed(null, session: true)).Model!;
        Assert.IsTrue(model.SessionUnavailable);
    }

    [TestMethod]
    public async Task Callback_Signin_ExternalItemsMissing_StartsSessionWithoutTokens()
    {
        var customer = new Customer { Id = "c2" };
        _externalItems.Clear();
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        await _controller.Callback("/");
        _sessions.Verify(x => x.Start(customer, "sub-1", null, null, "s1"), Times.Once);
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
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
        Marker("sub-1");
        var result = (RedirectToRouteResult)await _controller.LinkComplete("https://evil.example/");
        Assert.AreEqual("OidcLoginLogin", result.RouteName);
        Assert.IsNull(result.RouteValues!["returnUrl"]);
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
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_OtherSubjectMarker_BackToLink_NothingLinked()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        Marker("sub-other");
        Assert.AreEqual("OidcLoginLink", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LinkComplete_Success_RemovesMarker()
    {
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _external.Setup(x => x.Complete(_current, "s1")).ReturnsAsync(OidcLinkResult.Linked);
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
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task Callback_ProviderOnly_SignIn_SyncsProfile()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        var customer = new Customer { Id = "c9", Email = "john@example.com" };
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        await _controller.Callback("/");
        _customers.Verify(x => x.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, "John", ""), Times.Once);
        _customers.Verify(x => x.UpdateUserField(customer, SystemCustomerFieldNames.LastName, "Doe", ""), Times.Once);
        _auth.Verify(x => x.SignIn(customer, false, "session-1"), Times.Once);
    }

    [TestMethod]
    public async Task Callback_SyncThrows_StillSignsIn()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        var customer = new Customer { Id = "c9", Email = "john@example.com" };
        _customers.Setup(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException());
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        await _controller.Callback("/");
        _auth.Verify(x => x.SignIn(customer, false, "session-1"), Times.Once);
    }

    [TestMethod]
    public async Task Callback_Mixed_SignIn_NoSync()
    {
        var customer = new Customer { Id = "c9", Email = "john@example.com" };
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        _resolver.Setup(x => x.ResolveForLogin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .ReturnsAsync(OidcLoginResult.SignIn(customer));
        await _controller.Callback("/");
        _customers.Verify(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Link_ProviderOnly_Failed()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.Link("/")).RouteName);
        _external.Verify(x => x.Clear(), Times.Once);
    }

    [TestMethod]
    public async Task LinkComplete_ProviderOnly_Failed()
    {
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        _groups.Setup(x => x.IsRegistered(_current)).ReturnsAsync(true);
        _external.Setup(x => x.GetPending()).ReturnsAsync(Pending);
        Marker("sub-1");
        Assert.AreEqual("OidcLoginFailed", ((RedirectToRouteResult)await _controller.LinkComplete("/")).RouteName);
        _external.Verify(x => x.Complete(It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void Failed_EmailNotVerified_ShowsText()
    {
        var model = (FailedModel)((ViewResult)_controller.Failed((int)OidcLoginRefusal.EmailNotVerified)).Model!;
        Assert.AreEqual(OidcLoginRefusal.EmailNotVerified, model.Reason);
    }
}
