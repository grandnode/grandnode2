using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    private async Task Start(OidcResourceServerConfig config = null, params Store[] stores)
    {
        if (_host != null) await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(config ?? OidcTestHost.Config(),
            stores.Length > 0 ? s => s.AddSingleton(OidcTestHost.StoreService(stores)) : null);
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
    public async Task AccountConditionFlag_ValidToken_403()
    {
        await Start(OidcTestHost.Config(new OidcProtectedResource {
            Name = "BackendApi", PathPrefix = "/api", RequiredScopes = ["grandnode.api"], RequireAdminPanelAccess = true
        }));
        Assert.AreEqual(HttpStatusCode.Forbidden, (await Get("/api/ping", OidcTestHost.Token())).StatusCode);
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

    private async Task<string> MetadataResource(string path, string host = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (host != null) request.Headers.Host = host;
        using var response = await _client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK) return ((int)response.StatusCode).ToString();
        using var json = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("resource").GetString();
    }

    [TestMethod]
    public async Task ConfiguredResource_MetadataAtItsHostRoot()
    {
        await Start(OidcTestHost.Config(new OidcProtectedResource {
            Name = "BackendApi", PathPrefix = "/api", Resource = "https://api.x.test/v1", RequiredScopes = ["grandnode.api"]
        }));
        StringAssert.Contains((await Get("/api/ping")).Headers.WwwAuthenticate.ToString(),
            "resource_metadata=\"https://api.x.test/.well-known/oauth-protected-resource/v1\"");
        Assert.AreEqual("https://api.x.test/v1", await MetadataResource("/.well-known/oauth-protected-resource/v1"));
        Assert.AreEqual("404", await MetadataResource("/.well-known/oauth-protected-resource/api"));
    }

    [TestMethod]
    public async Task StoreUnderSubPath_MetadataPathCarriesStorePath()
    {
        await Start(null, new Store { Id = "store1", Url = "http://localhost/shop/" });
        StringAssert.Contains((await Get("/api/ping")).Headers.WwwAuthenticate.ToString(),
            "resource_metadata=\"http://localhost/.well-known/oauth-protected-resource/shop/api\"");
        Assert.AreEqual("http://localhost/shop/api", await MetadataResource("/.well-known/oauth-protected-resource/shop/api"));
        Assert.AreEqual("http://localhost/shop", await MetadataResource("/.well-known/oauth-protected-resource/shop"));
        Assert.AreEqual("404", await MetadataResource("/.well-known/oauth-protected-resource/api"));
    }

    [TestMethod]
    public async Task SameResourcePath_PickedByRequestAuthority()
    {
        await Start(OidcTestHost.Config(
            new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" },
            new OidcProtectedResource { Name = "Tools", PathPrefix = "/tools", Resource = "http://tools.test/api" }));
        Assert.AreEqual("http://localhost/api", await MetadataResource("/.well-known/oauth-protected-resource/api"));
        Assert.AreEqual("http://tools.test/api", await MetadataResource("/.well-known/oauth-protected-resource/api", "tools.test"));
    }

    [TestMethod]
    public async Task StoreWithoutUrl_Metadata404AndWarning_TokenRejected()
    {
        var logs = new ListLoggerProvider();
        await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(OidcTestHost.Config(), s =>
        {
            s.AddSingleton(OidcTestHost.StoreService(new Store { Id = "store1" }));
            s.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(logs);
        });

        Assert.AreEqual("404", await MetadataResource("/.well-known/oauth-protected-resource/api"));
        Assert.AreEqual("404", await MetadataResource("/.well-known/oauth-protected-resource"));
        Assert.IsTrue(logs.Messages.Any(m => m.Contains("BackendApi")), string.Join(" | ", logs.Messages));
        Assert.AreEqual(HttpStatusCode.Unauthorized, (await Get("/api/ping", OidcTestHost.Token())).StatusCode);
    }

    [TestMethod]
    public async Task AuthorizationServerUnreachable_503_NotInvalidToken()
    {
        var logs = new ListLoggerProvider();
        await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(OidcTestHost.Config(), s => s.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(logs),
            jwtBearer: o =>
            {
                //the configuration manager built from Authority, over a backchannel that cannot reach the identity provider
                o.Configuration = null;
                o.Backchannel = new HttpClient(new UnreachableHandler());
            });

        using var response = await Get("/api/ping", OidcTestHost.Token());
        var body = await response.Content.ReadAsStringAsync();

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode, body);
        Assert.IsFalse(response.Headers.WwwAuthenticate.ToString().Contains("invalid_token"));
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.IsTrue(logs.Messages.Any(m => m.StartsWith("Error:")), string.Join(" | ", logs.Messages));
    }

    [TestMethod]
    public async Task AuthorizationServerReachable_ForgedToken401_ValidToken200()
    {
        await _host.DisposeAsync();
        _host = new OidcTestHost();
        _client = await _host.Start(OidcTestHost.Config(), jwtBearer: o =>
        {
            o.Configuration = null;
            o.Backchannel = new HttpClient(new DiscoveryHandler());
        });

        using var forged = await Get("/api/ping", OidcTestHost.Token(key: OidcTestHost.ForeignKey));
        Assert.AreEqual(HttpStatusCode.Unauthorized, forged.StatusCode);
        StringAssert.Contains(forged.Headers.WwwAuthenticate.ToString(), "invalid_token");
        using var valid = await Get("/api/ping", OidcTestHost.Token());
        Assert.AreEqual(HttpStatusCode.OK, valid.StatusCode);
    }

    private sealed class DiscoveryHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.RequestUri!.AbsolutePath.EndsWith("/jwks")
                ? System.Text.Json.JsonSerializer.Serialize(new {
                    keys = new[] { JsonWebKeyConverter.ConvertFromRSASecurityKey(OidcTestHost.IdpKey) }
                        .Select(k => new { kty = k.Kty, kid = k.Kid, n = k.N, e = k.E, use = "sig" })
                })
                : $"{{\"issuer\":\"{OidcTestHost.Issuer}\",\"jwks_uri\":\"{OidcTestHost.Issuer}/jwks\"}}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private sealed class UnreachableHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("No such host is known.");
    }
}

internal sealed class ListLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider
{
    public System.Collections.Concurrent.ConcurrentQueue<string> Messages { get; } = new();
    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new Logger(this);
    public void Dispose() { }

    private sealed class Logger(ListLoggerProvider provider) : Microsoft.Extensions.Logging.ILogger
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId,
            TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            provider.Messages.Enqueue($"{logLevel}: {formatter(state, exception)}");
    }
}
