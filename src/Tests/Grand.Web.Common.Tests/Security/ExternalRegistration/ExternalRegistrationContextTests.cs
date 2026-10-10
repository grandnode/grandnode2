using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Web.Common.Security.ExternalRegistration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Security.ExternalRegistration;

[TestClass]
public class ExternalRegistrationContextTests
{
    private const string Scheme = "ExternalAuthentication"; //GrandCookieAuthenticationDefaults.ExternalAuthenticationScheme
    private Mock<IAuthenticationService> _auth;
    private Mock<IOidcCustomerResolver> _resolver;
    private DefaultHttpContext _http;

    private static readonly ExternalRegistrationIdentity Identity =
        new(OidcResourceServerDefaults.ProviderSystemName, "sub-1", "john@example.com", true, "John", "Doe");

    [TestInitialize]
    public void Init()
    {
        _auth = new Mock<IAuthenticationService>();
        _resolver = new Mock<IOidcCustomerResolver>();
        _http = new DefaultHttpContext {
            RequestServices = new ServiceCollection().AddSingleton(_auth.Object).BuildServiceProvider()
        };
    }

    private ExternalRegistrationContext Context() =>
        new(new HttpContextAccessor { HttpContext = _http }, _resolver.Object, NullLogger<ExternalRegistrationContext>.Instance);

    private void Cookie(ClaimsPrincipal principal) =>
        _auth.Setup(x => x.AuthenticateAsync(_http, Scheme)).ReturnsAsync(principal == null
            ? AuthenticateResult.NoResult()
            : AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme)));

    [TestMethod]
    public async Task RoundTrip()
    {
        Cookie(ExternalRegistrationContext.CreatePrincipal(Identity));
        Assert.AreEqual(Identity, await Context().GetPending());
    }

    [TestMethod]
    public async Task NoCookie_Null()
    {
        Cookie(null);
        Assert.IsNull(await Context().GetPending());
    }

    [TestMethod]
    public async Task OtherExternalLogin_Null()
    {
        //e.g. the Google handler signs its own principal into the same cookie
        Cookie(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Email, "x@y.z")], "Google")));
        Assert.IsNull(await Context().GetPending());
    }

    [TestMethod]
    public async Task UnverifiedFlag_RoundTrips()
    {
        Cookie(ExternalRegistrationContext.CreatePrincipal(Identity with { EmailVerified = false }));
        Assert.IsFalse((await Context().GetPending()).EmailVerified);
    }

    [TestMethod]
    public async Task Complete_Links_AndClears()
    {
        Cookie(ExternalRegistrationContext.CreatePrincipal(Identity));
        var customer = new Customer { Id = "c1" };
        _resolver.Setup(x => x.LinkIdentity(customer, "sub-1", "john@example.com")).ReturnsAsync(OidcLinkResult.Linked);
        Assert.AreEqual(OidcLinkResult.Linked, await Context().Complete(customer));
        _auth.Verify(x => x.SignOutAsync(_http, Scheme, It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [TestMethod]
    public async Task Complete_UnknownProvider_NotLinked_Cleared()
    {
        Cookie(ExternalRegistrationContext.CreatePrincipal(Identity with { ProviderSystemName = "Other" }));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Context().Complete(new Customer { Id = "c1" }));
        _resolver.Verify(x => x.LinkIdentity(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _auth.Verify(x => x.SignOutAsync(_http, Scheme, It.IsAny<AuthenticationProperties>()), Times.Once);
    }
}
