using Grand.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcPipelineTests
{
    private OidcTestHost _host;
    private HttpClient _client;

    private async Task Start(OidcResourceServerConfig config = null)
    {
        if (_host != null) await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(config ?? OidcTestHost.Config());
    }

    [TestInitialize]
    public Task Init() => Start();

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    [TestMethod]
    public async Task ForeignHostHeader_TokenForThatHost_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/ping");
        request.Headers.Host = "evil.test";
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", OidcTestHost.Token(audience: "http://evil.test/api"));
        var response = await _client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task RootMetadata_ReturnsDocumentBody()
    {
        var response = await _client.GetAsync("/.well-known/oauth-protected-resource");
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(body, "\"resource\":\"http://localhost\"");
        StringAssert.Contains(body, "\"authorization_servers\"");
    }

    [TestMethod]
    public async Task Disabled_OidcEndpoint_401NotServerError()
    {
        var config = OidcTestHost.Config();
        config.Enabled = false;
        await Start(config);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token())).StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping")).StatusCode);
    }

    private Task<HttpResponseMessage> Get(string path, string token = null, string scheme = "Bearer") =>
        OidcTestHost.Send(_client, path, token, scheme);

    [TestMethod]
    public async Task ValidToken_200()
    {
        var response = await Get("/api/ping", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("sub-1", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task NoToken_401_AdvertisesMetadataAndScope()
    {
        var response = await Get("/api/ping");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual("Bearer resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/api\", scope=\"grandnode.api\"",
            response.Headers.WwwAuthenticate.ToString());
    }

    [TestMethod]
    public async Task WrongAudience_401()
    {
        var response = await Get("/api/ping", OidcTestHost.Token(audience: "http://localhost/other"));
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        StringAssert.Contains(response.Headers.WwwAuthenticate.ToString(), "another audience");
    }

    [TestMethod]
    public async Task ConfiguredAudience_200()
    {
        await Start(OidcTestHost.Config(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api", Audiences = ["318274561"] }));
        Assert.AreEqual(HttpStatusCode.OK, (await Get("/api/ping", OidcTestHost.Token(audience: "318274561"))).StatusCode);
    }

    [TestMethod]
    public async Task AuthorizedParty_Enforced()
    {
        await Start(OidcTestHost.Config(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api", AuthorizedParties = ["cli"] }));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token(extra: new Claim("azp", "other")))).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await Get("/api/ping", OidcTestHost.Token(extra: new Claim("azp", "cli")))).StatusCode);
    }

    [TestMethod]
    public async Task MissingScope_403_InsufficientScope()
    {
        var response = await Get("/api/ping", OidcTestHost.Token(scope: "openid"));
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        var header = response.Headers.WwwAuthenticate.ToString();
        StringAssert.Contains(header, "error=\"insufficient_scope\"");
        StringAssert.Contains(header, "scope=\"grandnode.api\"");
        StringAssert.Contains(header, "resource_metadata=");
    }

    [TestMethod]
    public async Task ForgedSignature_401() =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token(key: OidcTestHost.ForeignKey))).StatusCode);

    [TestMethod]
    public async Task OtherIssuer_401() =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token(issuer: "https://evil.test"))).StatusCode);

    [TestMethod]
    public async Task Expired_401() =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token(expires: DateTime.UtcNow.AddMinutes(-5)))).StatusCode);

    [TestMethod]
    public async Task Hmac_401() =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token(
            key: new SymmetricSecurityKey(Encoding.ASCII.GetBytes(new string('k', 40))), algorithm: SecurityAlgorithms.HmacSha256))).StatusCode);

    [TestMethod]
    public async Task Unsigned_401()
    {
        var header = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");
        var payload = Base64UrlEncoder.Encode(
            $"{{\"iss\":\"{OidcTestHost.Issuer}\",\"aud\":\"http://localhost/api\",\"sub\":\"sub-1\",\"scope\":\"grandnode.api\",\"exp\":{DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()}}}");
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", $"{header}.{payload}.")).StatusCode);
    }

    [TestMethod]
    [DataRow("garbage", "Bearer")]
    [DataRow("dXNlcjpwYXNz", "Basic")]
    public async Task Malformed_401_Never500(string token, string scheme) =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", token, scheme)).StatusCode);

    [TestMethod]
    public async Task OidcEndpointOutsideAnyResource_401() =>
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/other/ping", OidcTestHost.Token(audience: "http://localhost/other"))).StatusCode);

    [TestMethod]
    public async Task Metadata_ServedAnd404ForUnknown()
    {
        var response = await _client.GetAsync("/.well-known/oauth-protected-resource/api");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        StringAssert.Contains(await response.Content.ReadAsStringAsync(), "\"resource\":\"http://localhost/api\"");
        Assert.AreEqual(HttpStatusCode.OK, (await _client.GetAsync("/.well-known/oauth-protected-resource")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await _client.GetAsync("/.well-known/oauth-protected-resource/other")).StatusCode);
    }
}
