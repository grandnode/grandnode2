using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcCustomerPipelineTests
{
    private OidcTestHost _host;
    private HttpClient _client;
    private Mock<IOidcCustomerResolver> _resolver;
    private Mock<IPermissionService> _permissions;

    private async Task Start(OidcResourceServerConfig config = null)
    {
        if (_host != null) await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(config ?? OidcTestHost.Config(),
            services: s =>
            {
                s.AddSingleton(_resolver.Object);
                s.AddSingleton(_permissions.Object);
            },
            endpoints: e =>
            {
                e.MapGet("/api/whoami", (HttpContext c) => (c.Items[OidcResourceServerDefaults.CustomerItemKey] as Customer)?.Id ?? "none")
                    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = OidcResourceServerDefaults.AuthenticationScheme });
                //no [Authorize]: the work context path - the request resolver authenticates on demand
                e.MapGet("/api/plain", async (HttpContext c) =>
                {
                    var resolver = c.RequestServices.GetServices<IRequestCustomerResolver>().Single();
                    return (await resolver.Resolve())?.Id ?? "none";
                });
            });
    }

    [TestInitialize]
    public async Task Init()
    {
        _resolver = new Mock<IOidcCustomerResolver>();
        _resolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(OidcCustomerResult.Success(new Customer { Id = "c1", Active = true }));
        _permissions = new Mock<IPermissionService>();
        _permissions.Setup(x => x.Authorize(It.IsAny<Permission>(), It.IsAny<Customer>())).ReturnsAsync(true);
        await Start();
    }

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    private Task<HttpResponseMessage> Get(string path, string token = null) => OidcTestHost.Send(_client, path, token);

    [TestMethod]
    public async Task ValidToken_CustomerInItems_StoreFromHost()
    {
        var response = await Get("/api/whoami", OidcTestHost.Token());
        Assert.AreEqual("c1", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), "store1"), Times.Once);
    }

    [TestMethod]
    public async Task ResolverInvalidToken_401_WithDescription()
    {
        _resolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(OidcCustomerResult.InvalidToken("The e-mail address in the token is not verified"));
        var response = await Get("/api/whoami", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "not verified");
    }

    [TestMethod]
    public async Task ResolverForbidden_403_NoWwwAuthenticate()
    {
        _resolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(OidcCustomerResult.Forbidden("The customer account is inactive"));
        var response = await Get("/api/whoami", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, response.Headers.WwwAuthenticate.Count);
    }

    [TestMethod]
    public async Task MissingScope_ResolverNeverCalled()
    {
        Assert.AreEqual(HttpStatusCode.Forbidden, (await Get("/api/whoami", OidcTestHost.Token(scope: "openid"))).StatusCode);
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task RequireAdminPanelAccess_NoPermission_403()
    {
        _permissions.Setup(x => x.Authorize(StandardPermission.ManageAccessAdminPanel, It.IsAny<Customer>())).ReturnsAsync(false);
        await Start(OidcTestHost.Config(new OidcProtectedResource {
            Name = "BackendApi", PathPrefix = "/api", RequireAdminPanelAccess = true
        }));
        Assert.AreEqual(HttpStatusCode.Forbidden, (await Get("/api/whoami", OidcTestHost.Token())).StatusCode);
    }

    [TestMethod]
    public async Task DenyVendors_VendorCustomer_403()
    {
        _resolver.Setup(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()))
            .ReturnsAsync(OidcCustomerResult.Success(new Customer { Id = "v", Active = true, VendorId = "vendor-1" }));
        await Start(OidcTestHost.Config(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api", DenyVendors = true }));
        Assert.AreEqual(HttpStatusCode.Forbidden, (await Get("/api/whoami", OidcTestHost.Token())).StatusCode);
    }

    [TestMethod]
    public async Task RequestCustomerResolver_WithAndWithoutToken()
    {
        Assert.AreEqual("c1", await (await Get("/api/plain", OidcTestHost.Token())).Content.ReadAsStringAsync());
        Assert.AreEqual("none", await (await Get("/api/plain")).Content.ReadAsStringAsync());
    }
}
