using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcForwardingPipelineTests
{
    private const string LegacySecret = "legacy-secret-key-at-least-32-characters";
    private OidcTestHost _host;
    private HttpClient _client;
    private Mock<IOidcCustomerResolver> _resolver;

    private async Task Start(OidcResourceServerConfig config, bool webApiDisabled = false)
    {
        if (_host != null) await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(config,
            services: s =>
            {
                s.AddSingleton(_resolver.Object);
                //what Grand.Module.Api registers while BackendAPI.Enabled is false
                if (webApiDisabled)
                    s.AddSingleton<IOidcDisabledPathProvider>(new DisabledPaths("/api"));
            },
            afterAuthorization: app => app.UseMiddleware<OidcAuthenticationMiddleware>(),
            auth: b => b.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters {
                    ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(LegacySecret))
                };
            }),
            endpoints: e =>
            {
                e.MapGet("/api/legacy", (HttpContext c) =>
                        c.User.FindFirst("sub")?.Value ?? c.User.FindFirst("Email")?.Value ?? "none")
                    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme });
                e.MapGet("/api/forbid", () => Results.Forbid(authenticationSchemes: [JwtBearerDefaults.AuthenticationScheme]))
                    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme });
                //the store ContextMiddleware builds the work context in
                e.MapGet("/api/legacy/store", (HttpContext c) => RequestStorePin.Get(c) ?? "none")
                    .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme });
                //no [Authorize]: the work context path
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
        await Start(OidcTestHost.Config());
    }

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    private static string LegacyToken() => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
        Subject = new ClaimsIdentity([new Claim("Email", "admin@example.com")]),
        Expires = DateTime.UtcNow.AddMinutes(10),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(LegacySecret)), SecurityAlgorithms.HmacSha256)
    });

    private Task<HttpResponseMessage> Get(string path, string token = null) => OidcTestHost.Send(_client, path, token);

    [TestMethod]
    public async Task IdpToken_OnBearerEndpoint_200()
    {
        using var response = await Get("/api/legacy", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("sub-1", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task LegacySecretKeyToken_200()
    {
        using var response = await Get("/api/legacy", LegacyToken());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("admin@example.com", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task IdpToken_OnBearerEndpoint_StorePinned()
    {
        using var response = await Get("/api/legacy/store", OidcTestHost.Token());
        Assert.AreEqual("store1", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task LegacySecretKeyToken_StoreNotPinned()
    {
        using var response = await Get("/api/legacy/store", LegacyToken());
        Assert.AreEqual("none", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task LegacySecretKeyToken_RequestCustomerResolver_NotOidc()
    {
        using var response = await Get("/api/plain", LegacyToken());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("none", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task NoToken_OnBearerEndpoint_401_OidcChallenge()
    {
        using var response = await Get("/api/legacy");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "resource_metadata");
    }

    [TestMethod]
    public async Task ThreeSegmentNonJwt_OnBearerEndpoint_401_OidcChallenge()
    {
        using var response = await Get("/api/legacy", "x.y.z");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "resource_metadata");
    }

    [TestMethod]
    public async Task ForgedIssuer_WrongKey_401()
    {
        using var response = await Get("/api/legacy", OidcTestHost.Token(key: OidcTestHost.ForeignKey));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task IdpToken_ForbidResult_403_NoError_Never500()
    {
        using var response = await Get("/api/forbid", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.IsFalse(response.Headers.WwwAuthenticate.ToString().Contains("error="), response.Headers.WwwAuthenticate.ToString());
    }

    [TestMethod]
    public async Task IdpSignedToken_OtherIssuer_401_InvalidToken_ResolverNotCalled()
    {
        using var response = await Get("/api/legacy", OidcTestHost.Token(issuer: "https://other.example.com"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "error=\"invalid_token\"");
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "issuer");
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task HmacToken_IdpIssuer_RejectedByBearer_ResolverNotCalled()
    {
        //HMAC-signed with a key that is not the SecretKey, claiming the provider as issuer: routed by its algorithm
        var token = OidcTestHost.Token(key: new SymmetricSecurityKey(Encoding.ASCII.GetBytes(new string('k', 40))),
            algorithm: SecurityAlgorithms.HmacSha256);
        using var response = await Get("/api/legacy", token);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsFalse(response.Headers.WwwAuthenticate.ToString().Contains("resource_metadata"),
            response.Headers.WwwAuthenticate.ToString());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("garbage")]
    [DataRow("")]
    public async Task Malformed_401_Never500(string token)
    {
        using var response = await Get("/api/legacy", token);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Disabled_IdpTokenRejected_LegacyAccepted()
    {
        var config = OidcTestHost.Config();
        config.Enabled = false;
        await Start(config);

        using var idpToken = await Get("/api/legacy", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Unauthorized, idpToken.StatusCode);
        using var legacyToken = await Get("/api/legacy", LegacyToken());
        Assert.AreEqual(HttpStatusCode.OK, legacyToken.StatusCode);
    }

    private class DisabledPaths(params string[] paths) : IOidcDisabledPathProvider
    {
        public IEnumerable<string> GetDisabledPaths() => paths;
    }

    [TestMethod]
    public async Task WebApiDisabled_IdpToken_RefusedBeforeCustomerResolution()
    {
        await Start(OidcTestHost.Config(), webApiDisabled: true);

        using var bearerEndpoint = await Get("/api/legacy", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Unauthorized, bearerEndpoint.StatusCode);
        using var pinned = await Get("/api/legacy/store", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Unauthorized, pinned.StatusCode);
        //no [Authorize]: neither OidcAuthenticationMiddleware nor the work context maps the token
        using var plain = await Get("/api/plain", OidcTestHost.Token());
        Assert.AreEqual("none", await plain.Content.ReadAsStringAsync());

        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task WebApiDisabled_ResourceNotPublished()
    {
        await Start(OidcTestHost.Config(), webApiDisabled: true);

        using var response = await Get("/.well-known/oauth-protected-resource/api");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string Unsigned(string payloadJson)
    {
        static string B64(string json) => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return B64("{\"alg\":\"none\"}") + "." + B64(payloadJson) + ".";
    }

    [TestMethod]
    [DataRow("{\"iss\":123}")]
    [DataRow("{\"iss\":{\"a\":1}}")]
    [DataRow("{\"iss\":[\"a\",\"b\"]}")]
    [DataRow("[1,2]")]
    [DataRow("null")]
    public async Task MalformedIssuerPayload_Never500(string payload)
    {
        var token = Unsigned(payload);
        var legacy = await Get("/api/legacy", token);
        Assert.IsTrue((int)legacy.StatusCode < 500, "legacy " + legacy.StatusCode);
        var plain = await Get("/api/plain", token);
        Assert.AreEqual(HttpStatusCode.OK, plain.StatusCode);
        Assert.AreEqual("none", await plain.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task Anonymous_PlainEndpoint_NoChallenge_ResolverNotCalled()
    {
        using var response = await Get("/api/plain");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("none", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }
}
