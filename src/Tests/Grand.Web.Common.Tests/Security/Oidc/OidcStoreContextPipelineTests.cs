using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Common.Middleware;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Security.Oidc;

/// <summary>
///     The store of the work context on a protected resource: the store the token was validated for, whatever store
///     cookie the client also sends; without a validated token the cookie comes first, as before
/// </summary>
[TestClass]
public class OidcStoreContextPipelineTests
{
    private static readonly Store HostStore = new() {
        Id = "store1", Url = "http://localhost/", Domains = { new DomainHost { HostName = "localhost", Url = "http://localhost/" } }
    };

    private static readonly Store CookieStore = new() {
        Id = "store2", Url = "http://shop2.test/", Domains = { new DomainHost { HostName = "shop2.test", Url = "http://shop2.test/" } }
    };

    private OidcTestHost _host;
    private HttpClient _client;
    private Mock<IOidcCustomerResolver> _resolver;
    private Mock<IWorkContextSetter> _workContextSetter;

    [TestInitialize]
    public async Task Init()
    {
        _resolver = new Mock<IOidcCustomerResolver>();
        _resolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(OidcCustomerResult.Success(new Customer { Id = "c1", Active = true }));
        _workContextSetter = new Mock<IWorkContextSetter>();
        _workContextSetter.Setup(x => x.InitializeWorkContext(It.IsAny<string>())).ReturnsAsync(Mock.Of<IWorkContext>());

        _host = new OidcTestHost();
        _client = await _host.Start(OidcTestHost.Config(),
            services: s =>
            {
                s.AddSingleton(_resolver.Object);
                s.AddSingleton(OidcTestHost.StoreService(HostStore, CookieStore));
                s.AddScoped<IStoreContextSetter, StoreContextSetter>();
                s.AddSingleton(_workContextSetter.Object);
                s.AddSingleton<IContextAccessor, ContextAccessor>();
            },
            endpoints: e =>
            {
                e.MapGet("/api/store", StoreOfTheWorkContext)
                    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = OidcResourceServerDefaults.AuthenticationScheme });
                //no [Authorize]: the token is authenticated on demand, not by the authorization middleware
                e.MapGet("/api/plain/store", StoreOfTheWorkContext);
                e.MapGet("/other/store", StoreOfTheWorkContext);
            },
            afterAuthorization: app =>
            {
                //as UseGrandAuthentication and AuthenticationStartup order them
                app.UseMiddleware<OidcAuthenticationMiddleware>();
                app.UseMiddleware<ContextMiddleware>();
            });
    }

    private static string StoreOfTheWorkContext(HttpContext context) =>
        context.RequestServices.GetRequiredService<IContextAccessor>().StoreContext.CurrentStore.Id;

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    private async Task<string> Get(string path, string token = null, string storeCookie = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (storeCookie != null) request.Headers.Add("Cookie", $"{CommonHelper.StoreCookieName}={storeCookie}");
        using var response = await _client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    [TestMethod]
    [DataRow("/api/store")]
    [DataRow("/api/plain/store")]
    public async Task ValidToken_AnotherStoresCookie_WorkContextInTheTokensStore(string path)
    {
        Assert.AreEqual("store1", await Get(path, OidcTestHost.Token(), CookieStore.Id));
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), "store1"), Times.Once);
        _workContextSetter.Verify(x => x.InitializeWorkContext("store1"), Times.Once);
    }

    [TestMethod]
    public async Task NoToken_StoreCookieFirst()
    {
        Assert.AreEqual("store2", await Get("/api/plain/store", storeCookie: CookieStore.Id));
        Assert.AreEqual("store1", await Get("/api/plain/store"));
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task InvalidToken_StoreCookieFirst()
    {
        var expired = OidcTestHost.Token(expires: DateTime.UtcNow.AddMinutes(-10));
        Assert.AreEqual("store2", await Get("/api/plain/store", expired, CookieStore.Id));
    }

    [TestMethod]
    public async Task RefusedCustomer_StoreCookieFirst()
    {
        _resolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(OidcCustomerResult.Forbidden("The customer account is inactive"));
        Assert.AreEqual("store2", await Get("/api/plain/store", OidcTestHost.Token(), CookieStore.Id));
    }

    [TestMethod]
    public async Task OutsideProtectedResources_TokenIgnored_StoreCookieFirst()
    {
        Assert.AreEqual("store2", await Get("/other/store", OidcTestHost.Token(), CookieStore.Id));
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }
}
