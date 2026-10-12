using Authentication.OpenIdConnect;
using Authentication.OpenIdConnect.Infrastructure;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Cryptography;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcBackChannelLogoutEndpointTests
{
    private const string Issuer = "https://idp.test/realms/shop";
    private static readonly RsaSecurityKey Key = NewKey();
    private Mock<IOidcSessionService> _sessions;
    private Mock<IOidcTokenRevocationService> _revocation;
    private OpenIdConnectOptions _options;
    private bool _schemeRegistered;

    private static RsaSecurityKey NewKey()
    {
        using var rsa = RSA.Create(2048);
        return new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = "k1" };
    }

    [TestInitialize]
    public void Init()
    {
        _sessions = new Mock<IOidcSessionService>();
        _sessions.Setup(x => x.EndBySid(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync([]);
        _sessions.Setup(x => x.EndBySubject(It.IsAny<string>())).ReturnsAsync([]);
        _revocation = new Mock<IOidcTokenRevocationService>();
        var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
        configuration.SigningKeys.Add(Key);
        _options = new OpenIdConnectOptions {
            ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration)
        };
        _schemeRegistered = true;
    }

    private static string Token(string sub = "sub-1", string sid = "sid-1")
    {
        var claims = new Dictionary<string, object> {
            ["iss"] = Issuer, ["aud"] = "shop", ["iat"] = EpochTime.GetIntDate(DateTime.UtcNow),
            ["jti"] = Guid.NewGuid().ToString("N"),
            ["events"] = new Dictionary<string, object> { [OidcLogoutTokenValidator.BackChannelLogoutEvent] = new Dictionary<string, object>() }
        };
        if (sub != null) claims["sub"] = sub;
        if (sid != null) claims["sid"] = sid;
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor {
            Claims = claims, SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.RsaSha256), TokenType = "logout+jwt"
        });
    }

    private async Task<HttpContext> Post(string logoutToken)
    {
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        schemes.Setup(x => x.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme)).ReturnsAsync(() => _schemeRegistered
            ? new AuthenticationScheme(OidcLoginDefaults.AuthenticationScheme, null, typeof(IAuthenticationHandler))
            : null);
        var monitor = new Mock<IOptionsMonitor<OpenIdConnectOptions>>();
        monitor.Setup(x => x.Get(OidcLoginDefaults.AuthenticationScheme)).Returns(_options);
        var services = new ServiceCollection()
            .AddSingleton(schemes.Object)
            .AddSingleton(monitor.Object)
            .AddSingleton(new OidcLogoutTokenValidator(new MemoryCache(new MemoryCacheOptions())))
            .AddSingleton(_sessions.Object)
            .AddSingleton(_revocation.Object)
            .AddSingleton(new OidcLoginConfig { ClientId = "shop" })
            .AddSingleton(new OidcResourceServerConfig { ClockSkewSeconds = 60 })
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "POST";
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(logoutToken == null ? [] : new() { ["logout_token"] = logoutToken });
        context.Response.Body = new MemoryStream();
        var result = await OidcBackChannelLogoutEndpoint.Handle(context);
        await result.ExecuteAsync(context);
        return context;
    }

    [TestMethod]
    public async Task SubAndSid_EndsMatchingSessions_RevokesSubject()
    {
        var context = await Post(Token());
        Assert.AreEqual(200, context.Response.StatusCode);
        Assert.AreEqual("no-store", context.Response.Headers.CacheControl.ToString());
        _sessions.Verify(x => x.EndBySid("sub-1", "sid-1"), Times.Once);
        _revocation.Verify(x => x.RevokeTokens("sub-1"), Times.Once);
    }

    //the subject comes from the ended sessions
    [TestMethod]
    public async Task SidOnly_RevokesSubjectsOfEndedSessions()
    {
        _sessions.Setup(x => x.EndBySid(null, "sid-1")).ReturnsAsync([
            new OidcSession { Subject = "sub-1" }, new OidcSession { Subject = "sub-1" }, new OidcSession { Subject = "sub-2" }
        ]);
        await Post(Token(sub: null));
        _revocation.Verify(x => x.RevokeTokens("sub-1"), Times.Once);
        _revocation.Verify(x => x.RevokeTokens("sub-2"), Times.Once);
    }

    [TestMethod]
    public async Task SubOnly_EndsAllSessionsOfSubject()
    {
        await Post(Token(sid: null));
        _sessions.Verify(x => x.EndBySubject("sub-1"), Times.Once);
        _revocation.Verify(x => x.RevokeTokens("sub-1"), Times.Once);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("not-a-token")]
    public async Task MissingOrInvalidToken_400_NothingEnded(string token)
    {
        var context = await Post(token);
        Assert.AreEqual(400, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        StringAssert.Contains(await new StreamReader(context.Response.Body).ReadToEndAsync(), "invalid_request");
        _sessions.VerifyNoOtherCalls();
        _revocation.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task NotConfigured_400()
    {
        _schemeRegistered = false;
        Assert.AreEqual(400, (await Post(Token())).Response.StatusCode);
    }

    [TestMethod]
    public async Task MetadataUnobtainable_503()
    {
        var manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        manager.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("IDX20803"));
        _options.ConfigurationManager = manager.Object;
        Assert.AreEqual(503, (await Post(Token())).Response.StatusCode);
    }
}
