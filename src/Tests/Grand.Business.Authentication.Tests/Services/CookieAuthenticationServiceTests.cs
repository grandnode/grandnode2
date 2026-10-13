using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class CookieAuthenticationServiceTests
{
    private Mock<IAuthenticationService> _authServiceMock;
    private SecurityConfig _config;
    private CookieAuthenticationService _cookieAuthService;
    private Mock<ICustomerService> _customerServiceMock;
    private CustomerSettings _customerSettings;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IHttpContextAccessor> _httpAccessorMock;
    private DefaultHttpContext _httpContext;
    private Mock<IServiceProvider> serviceProviderMock;
    private CookieOptionsFactory _cookieOptionsFactory;
    private Mock<IOidcSessionService> _oidcSessionService;
    private Mock<IExternalSignInPolicy> _signInPolicy;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();

        _httpAccessorMock = new Mock<IHttpContextAccessor>();
        _customerSettings = new CustomerSettings();
        _groupServiceMock = new Mock<IGroupService>();
        _config = new SecurityConfig {
            CookieClaimsIssuer = "grandnode",
            CookiePrefix = ".Grand."
        };
        _cookieOptionsFactory = new CookieOptionsFactory(_config);
        _oidcSessionService = new Mock<IOidcSessionService>();
        _signInPolicy = new Mock<IExternalSignInPolicy>();
        _cookieAuthService = new CookieAuthenticationService(_customerSettings, _customerServiceMock.Object,
            _groupServiceMock.Object, _httpAccessorMock.Object, _cookieOptionsFactory, _config,
            _oidcSessionService.Object, _signInPolicy.Object, NullLogger<CookieAuthenticationService>.Instance);
        //For mock HttpContext extension methods like SignOutAsync ,SignInAsync etc..
        _authServiceMock = new Mock<IAuthenticationService>();
        serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(_ => _.GetService(typeof(IAuthenticationService)))
            .Returns(_authServiceMock.Object);
        _httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };
        _httpAccessorMock.Setup(c => c.HttpContext).Returns(_httpContext);
    }

    [TestMethod]
    public async Task SignOut()
    {
        _authServiceMock.Setup(c =>
            c.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()));
        await _cookieAuthService.SignOut();
        _authServiceMock.Verify(
            c => c.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()),
            Times.AtLeastOnce);
    }


    [TestMethod]
    public async Task SignIn_NullCustomer_ThrowException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            await _cookieAuthService.SignIn(null, false));
    }


    [TestMethod]
    public async Task SignIn_ValidCustomer()
    {
        _authServiceMock.Setup(c => c.SignInAsync(It.IsAny<HttpContext>(),
                It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(() => Task.CompletedTask);
        await _cookieAuthService.SignIn(new Customer(), false);
        _authServiceMock.Verify(c => c.SignInAsync(It.IsAny<HttpContext>(),
            It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_AuthenticatedFaild_ReturnNull()
    {
        _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult(AuthenticateResult.Fail(new Exception())));
        var customer = await _cookieAuthService.GetAuthenticatedCustomer();
        Assert.IsNull(customer);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_UsernameEnableRegisterd_ReturnCustomer()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };

        _customerSettings.UsernamesEnabled = true;
        var cliaim = new Claim(ClaimTypes.Name, "Johny", "", "grandnode");
        IList<Claim> claims = new List<Claim> {
            new(ClaimTypes.Name, ClaimTypes.Name, "", "grandnode")
        };
        var principals =
            new ClaimsPrincipal(new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme));
        _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principals, ""))));
        _customerServiceMock.Setup(c => c.GetCustomerByUsername(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));

        var customer = await _cookieAuthService.GetAuthenticatedCustomer();
        Assert.AreEqual(customer.Username, expectedCustomer.Username);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_WithCustomerIdClaim_ResolvesById_ReturnCustomer()
    {
        var expectedCustomer = new Customer { Id = "cust-1", Email = "john@grand.com", Active = true };
        var claims = new List<Claim> {
            new("grand:customerId", "cust-1", ClaimValueTypes.String, "grandnode")
        };
        var principals =
            new ClaimsPrincipal(new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme));
        _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principals, ""))));
        _customerServiceMock.Setup(c => c.GetCustomerById("cust-1")).ReturnsAsync(expectedCustomer);
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(true));

        var customer = await _cookieAuthService.GetAuthenticatedCustomer();

        Assert.IsNotNull(customer);
        Assert.AreEqual("cust-1", customer.Id);
        _customerServiceMock.Verify(c => c.GetCustomerById("cust-1"), Times.Once);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_UsernameEnableGuests_ReturnNull()
    {
        var expectedCustomer = new Customer { Username = "John", Active = true };
        _customerSettings.UsernamesEnabled = true;
        var cliaim = new Claim(ClaimTypes.Name, "Johny", "", "grandnode");
        IList<Claim> claims = new List<Claim> {
            new(ClaimTypes.Name, ClaimTypes.Name, "", "grandnode")
        };
        var principals =
            new ClaimsPrincipal(new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme));
        _authServiceMock.Setup(c => c.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns(() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principals, ""))));
        _customerServiceMock.Setup(c => c.GetCustomerByUsername(It.IsAny<string>()))
            .Returns(() => Task.FromResult(expectedCustomer));
        //guest
        _groupServiceMock.Setup(c => c.IsRegistered(It.IsAny<Customer>())).Returns(() => Task.FromResult(false));
        var customer = await _cookieAuthService.GetAuthenticatedCustomer();
        Assert.IsNull(customer);
        //_customerServiceMock.Verify(c => c.GetCustomerByUsername(It.IsAny<string>()), Times.Once);
    }

    private static AuthenticateResult Ticket(params Claim[] extra)
    {
        var claims = new List<Claim> {
            new("grand:customerId", "c1", ClaimValueTypes.String, "grandnode"),
            new(ClaimTypes.UserData, "token", ClaimValueTypes.String, "grandnode")
        };
        claims.AddRange(extra);
        return AuthenticateResult.Success(new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, GrandCookieAuthenticationDefaults.AuthenticationScheme)),
            GrandCookieAuthenticationDefaults.AuthenticationScheme));
    }

    private Customer RegisteredCustomer()
    {
        var customer = new Customer { Id = "c1", Active = true };
        customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.PasswordToken, Value = "token", StoreId = "" });
        _customerServiceMock.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(customer);
        _groupServiceMock.Setup(x => x.IsRegistered(customer)).ReturnsAsync(true);
        return customer;
    }

    [TestMethod]
    public async Task SignIn_WithOidcSession_AddsClaim()
    {
        ClaimsPrincipal signedIn = null;
        _authServiceMock.Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Callback((HttpContext _, string _, ClaimsPrincipal p, AuthenticationProperties _) => signedIn = p)
            .Returns(Task.CompletedTask);
        var customer = new Customer { Id = "c1", Email = "a@b.c" };
        customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.PasswordToken, Value = "token", StoreId = "" });

        await _cookieAuthService.SignIn(customer, false, "session-1");

        var claim = signedIn.FindFirst(OidcSessionDefaults.SessionIdClaimType);
        Assert.AreEqual("session-1", claim.Value);
        Assert.AreEqual("grandnode", claim.Issuer);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_NoSessionClaim_SessionServiceNotAsked()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>())).ReturnsAsync(Ticket());
        Assert.AreSame(customer, await _cookieAuthService.GetAuthenticatedCustomer());
        _oidcSessionService.Verify(x => x.Validate(It.IsAny<string>(), It.IsAny<Customer>()), Times.Never);
    }

    //an account that must sign in through the provider: a cookie without a session (issued before the account was
    //connected or before SsoOnly) would escape revalidation and back-channel logout
    [TestMethod]
    public async Task GetAuthenticatedCustomer_NoSessionClaim_ProviderRequired_Null()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>())).ReturnsAsync(Ticket());
        _signInPolicy.Setup(x => x.RequiredProvider(customer)).ReturnsAsync(Mock.Of<IExternalAuthenticationProvider>());
        Assert.IsNull(await _cookieAuthService.GetAuthenticatedCustomer());
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_NoSessionClaim_PasswordAllowed_ReturnCustomer()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>())).ReturnsAsync(Ticket());
        _signInPolicy.Setup(x => x.RequiredProvider(customer)).ReturnsAsync((IExternalAuthenticationProvider)null);
        Assert.AreSame(customer, await _cookieAuthService.GetAuthenticatedCustomer());
        _signInPolicy.Verify(x => x.RequiredProvider(customer), Times.Once);
    }

    //a session cookie is decided by its session; the policy is not asked
    [TestMethod]
    public async Task GetAuthenticatedCustomer_SessionClaim_PolicyNotAsked()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));
        _oidcSessionService.Setup(x => x.Validate("session-1", customer)).ReturnsAsync(true);
        Assert.AreSame(customer, await _cookieAuthService.GetAuthenticatedCustomer());
        _signInPolicy.Verify(x => x.RequiredProvider(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_EndedSession_Null()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));
        _oidcSessionService.Setup(x => x.Validate("session-1", customer)).ReturnsAsync(false);
        Assert.IsNull(await _cookieAuthService.GetAuthenticatedCustomer());
    }

    [TestMethod]
    public async Task GetAuthenticatedCustomer_ValidSession_ReturnCustomer()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));
        _oidcSessionService.Setup(x => x.Validate("session-1", customer)).ReturnsAsync(true);
        Assert.AreSame(customer, await _cookieAuthService.GetAuthenticatedCustomer());
    }

    //a claim not issued by the cookie (another issuer) is not a session reference
    [TestMethod]
    public async Task GetAuthenticatedCustomer_SessionClaimOfOtherIssuer_Ignored()
    {
        var customer = RegisteredCustomer();
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "other")));
        Assert.AreSame(customer, await _cookieAuthService.GetAuthenticatedCustomer());
        _oidcSessionService.Verify(x => x.Validate(It.IsAny<string>(), It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task SignOut_EndsOidcSession()
    {
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));
        await _cookieAuthService.SignOut();
        _oidcSessionService.Verify(x => x.End("session-1", OidcSessionEndReason.SignOut), Times.Once);
    }

    private (Customer customer, Func<ClaimsPrincipal> signedIn) PrepareSignIn(string customerId)
    {
        ClaimsPrincipal signedIn = null;
        _authServiceMock.Setup(x => x.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Callback((HttpContext _, string _, ClaimsPrincipal p, AuthenticationProperties _) => signedIn = p)
            .Returns(Task.CompletedTask);
        var customer = new Customer { Id = customerId, Email = "a@b.c" };
        customer.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.PasswordToken, Value = "token", StoreId = "" });
        return (customer, () => signedIn);
    }

    //re-sign-in (password, e-mail or username change) keeps the session of a provider sign-in
    [TestMethod]
    public async Task SignIn_WithoutSessionId_SameCustomerCookieWithSession_CarriesSessionOver()
    {
        var (customer, signedIn) = PrepareSignIn("c1");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));

        await _cookieAuthService.SignIn(customer, false);

        var claim = signedIn().FindFirst(OidcSessionDefaults.SessionIdClaimType);
        Assert.IsNotNull(claim);
        Assert.AreEqual("session-1", claim.Value);
        Assert.AreEqual("grandnode", claim.Issuer);
    }

    [TestMethod]
    public async Task SignIn_WithoutSessionId_OtherCustomerCookieWithSession_NoCarryOver()
    {
        var (customer, signedIn) = PrepareSignIn("c2");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));

        await _cookieAuthService.SignIn(customer, false);

        Assert.IsNull(signedIn().FindFirst(OidcSessionDefaults.SessionIdClaimType));
    }

    [TestMethod]
    public async Task SignIn_WithoutSessionId_SessionClaimOfOtherIssuer_NoCarryOver()
    {
        var (customer, signedIn) = PrepareSignIn("c1");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "other")));

        await _cookieAuthService.SignIn(customer, false);

        Assert.IsNull(signedIn().FindFirst(OidcSessionDefaults.SessionIdClaimType));
    }

    //signing in through the provider again: the replaced session is ended, not left open with its refresh token
    [TestMethod]
    public async Task SignIn_NewSession_SameCustomerCookieWithOtherSession_EndsPrevious()
    {
        var (customer, signedIn) = PrepareSignIn("c1");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));

        await _cookieAuthService.SignIn(customer, false, "session-2");

        _oidcSessionService.Verify(x => x.End("session-1", OidcSessionEndReason.SignOut), Times.Once);
        Assert.AreEqual("session-2", signedIn().FindFirst(OidcSessionDefaults.SessionIdClaimType)!.Value);
    }

    [TestMethod]
    public async Task SignIn_NewSession_OtherCustomerCookie_PreviousNotEnded()
    {
        var (customer, _) = PrepareSignIn("c2");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));

        await _cookieAuthService.SignIn(customer, false, "session-2");

        _oidcSessionService.Verify(x => x.End(It.IsAny<string>(), It.IsAny<OidcSessionEndReason>()), Times.Never);
    }

    [TestMethod]
    public async Task SignIn_WithoutSessionId_NoCurrentCookie_NoClaim()
    {
        var (customer, signedIn) = PrepareSignIn("c1");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(AuthenticateResult.NoResult());

        await _cookieAuthService.SignIn(customer, false);

        Assert.IsNull(signedIn().FindFirst(OidcSessionDefaults.SessionIdClaimType));
    }

    [TestMethod]
    public async Task SignIn_WithoutSessionId_CurrentCookieWithoutSession_NoClaim()
    {
        var (customer, signedIn) = PrepareSignIn("c1");
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket());

        await _cookieAuthService.SignIn(customer, false);

        Assert.IsNull(signedIn().FindFirst(OidcSessionDefaults.SessionIdClaimType));
    }

    [TestMethod]
    public async Task SignOut_NoSessionClaim_EndNeverCalled()
    {
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket());
        await _cookieAuthService.SignOut();
        _oidcSessionService.Verify(x => x.End(It.IsAny<string>(), It.IsAny<OidcSessionEndReason>()), Times.Never);
        _authServiceMock.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    //a failing session store must not keep the customer signed in
    [TestMethod]
    public async Task SignOut_EndThrows_StillSignsOut()
    {
        _authServiceMock.Setup(x => x.AuthenticateAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme))
            .ReturnsAsync(Ticket(new Claim(OidcSessionDefaults.SessionIdClaimType, "session-1", ClaimValueTypes.String, "grandnode")));
        _oidcSessionService.Setup(x => x.End("session-1", OidcSessionEndReason.SignOut))
            .ThrowsAsync(new InvalidOperationException("database down"));

        await _cookieAuthService.SignOut();

        _authServiceMock.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.AuthenticationScheme,
            It.IsAny<AuthenticationProperties>()), Times.Once);
        _authServiceMock.Verify(x => x.SignOutAsync(It.IsAny<HttpContext>(), GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme,
            It.IsAny<AuthenticationProperties>()), Times.Once);
    }
}
