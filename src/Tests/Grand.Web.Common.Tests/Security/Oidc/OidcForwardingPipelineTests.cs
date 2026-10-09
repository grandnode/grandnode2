using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
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

    private async Task Start(OidcResourceServerConfig config)
    {
        if (_host != null) await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(config,
            services: s => s.AddSingleton(_resolver.Object),
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
        var response = await Get("/api/legacy", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("sub-1", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task LegacySecretKeyToken_200()
    {
        var response = await Get("/api/legacy", LegacyToken());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("admin@example.com", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task LegacySecretKeyToken_RequestCustomerResolver_NotOidc()
    {
        var response = await Get("/api/plain", LegacyToken());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("none", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task NoToken_OnBearerEndpoint_401_OidcChallenge()
    {
        var response = await Get("/api/legacy");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "resource_metadata");
    }

    [TestMethod]
    public async Task ThreeSegmentNonJwt_OnBearerEndpoint_401_OidcChallenge()
    {
        var response = await Get("/api/legacy", "x.y.z");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "resource_metadata");
    }

    [TestMethod]
    public async Task ForgedIssuer_WrongKey_401()
    {
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/legacy", OidcTestHost.Token(key: OidcTestHost.ForeignKey))).StatusCode);
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task IdpToken_ForbidResult_403_NoError_Never500()
    {
        var response = await Get("/api/forbid", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.IsFalse(response.Headers.WwwAuthenticate.ToString().Contains("error="), response.Headers.WwwAuthenticate.ToString());
    }

    [TestMethod]
    public async Task IdpSignedToken_OtherIssuer_401_InvalidToken_ResolverNotCalled()
    {
        var response = await Get("/api/legacy", OidcTestHost.Token(issuer: "https://other.example.com"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "error=\"invalid_token\"");
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "issuer");
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    [DataRow("garbage")]
    [DataRow("")]
    public async Task Malformed_401_Never500(string token) =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/legacy", token)).StatusCode);

    [TestMethod]
    public async Task Disabled_IdpTokenRejected_LegacyAccepted()
    {
        var config = OidcTestHost.Config();
        config.Enabled = false;
        await Start(config);

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/legacy", OidcTestHost.Token())).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Get("/api/legacy", LegacyToken())).StatusCode);
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
        var response = await Get("/api/plain");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("none", await response.Content.ReadAsStringAsync());
        _resolver.Verify(x => x.Resolve(It.IsAny<ClaimsPrincipal>(), It.IsAny<string>()), Times.Never);
    }
}
