using Authentication.OpenIdConnect.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcLogoutTokenValidatorTests
{
    private const string Issuer = "https://idp.test/realms/shop";
    private const string ClientId = "shop";
    private static readonly RsaSecurityKey Key = NewKey();
    private static readonly RsaSecurityKey OtherKey = NewKey();
    private OidcLogoutTokenValidator _validator;
    private OpenIdConnectConfiguration _configuration;

    private static RsaSecurityKey NewKey()
    {
        using var rsa = RSA.Create(2048);
        return new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = Guid.NewGuid().ToString("N") };
    }

    [TestInitialize]
    public void Init()
    {
        _validator = new OidcLogoutTokenValidator(new MemoryCache(new MemoryCacheOptions()));
        _configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
        _configuration.SigningKeys.Add(Key);
    }

    private static string Token(Dictionary<string, object> claims = null, string typ = "logout+jwt", SecurityKey key = null,
        string algorithm = SecurityAlgorithms.RsaSha256, string issuer = Issuer, object audience = null,
        DateTime? issuedAt = null, bool events = true, string jti = null)
    {
        var payload = new Dictionary<string, object> {
            ["iss"] = issuer,
            ["aud"] = audience ?? ClientId,
            ["iat"] = EpochTime.GetIntDate(issuedAt ?? DateTime.UtcNow),
            ["jti"] = jti ?? Guid.NewGuid().ToString("N"),
            ["sub"] = "sub-1",
            ["sid"] = "sid-1"
        };
        if (events)
            payload["events"] = new Dictionary<string, object> { [OidcLogoutTokenValidator.BackChannelLogoutEvent] = new Dictionary<string, object>() };
        foreach (var (name, value) in claims ?? [])
            if (value == null) payload.Remove(name); else payload[name] = value;

        var descriptor = new SecurityTokenDescriptor {
            Claims = payload,
            SigningCredentials = new SigningCredentials(key ?? Key, algorithm),
            TokenType = typ
        };
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(descriptor);
    }

    private Task<OidcLogoutToken> Validate(string token) =>
        _validator.Validate(token, _configuration, ClientId, TimeSpan.FromSeconds(60));

    [TestMethod]
    [DataRow("logout+jwt")]
    [DataRow("JWT")]
    public async Task Valid_BothTypes(string typ)
    {
        var result = await Validate(Token(typ: typ));
        Assert.IsTrue(result.IsValid, result.Error);
        Assert.AreEqual("sub-1", result.Subject);
        Assert.AreEqual("sid-1", result.Sid);
    }

    //Zitadel sends aud as an array
    [TestMethod]
    public async Task Valid_AudienceArray() =>
        Assert.IsTrue((await Validate(Token(audience: new[] { ClientId, "other" }))).IsValid);

    [TestMethod]
    public async Task Valid_SidOnly()
    {
        var result = await Validate(Token(new() { ["sub"] = null }));
        Assert.IsTrue(result.IsValid, result.Error);
        Assert.IsNull(result.Subject);
    }

    [TestMethod]
    public async Task Valid_SubOnly()
    {
        var result = await Validate(Token(new() { ["sid"] = null }));
        Assert.IsTrue(result.IsValid, result.Error);
        Assert.IsNull(result.Sid);
    }

    [TestMethod]
    public async Task Invalid_NeitherSubNorSid() =>
        Assert.IsFalse((await Validate(Token(new() { ["sub"] = null, ["sid"] = null }))).IsValid);

    [TestMethod]
    public async Task Invalid_NoEvents() => Assert.IsFalse((await Validate(Token(events: false))).IsValid);

    [TestMethod]
    public async Task Invalid_OtherEvent() =>
        Assert.IsFalse((await Validate(Token(new() {
            ["events"] = new Dictionary<string, object> { ["urn:other"] = new Dictionary<string, object>() }
        }, events: false))).IsValid);

    //an ID token replayed as a logout token
    [TestMethod]
    public async Task Invalid_Nonce() => Assert.IsFalse((await Validate(Token(new() { ["nonce"] = "n" }))).IsValid);

    [TestMethod]
    public async Task Invalid_OldIat() =>
        Assert.IsFalse((await Validate(Token(issuedAt: DateTime.UtcNow.AddMinutes(-10)))).IsValid);

    [TestMethod]
    public async Task Invalid_FutureIat() =>
        Assert.IsFalse((await Validate(Token(issuedAt: DateTime.UtcNow.AddMinutes(10)))).IsValid);

    [TestMethod]
    public async Task Invalid_NoIat() => Assert.IsFalse((await Validate(Token(new() { ["iat"] = null }))).IsValid);

    [TestMethod]
    public async Task Invalid_Expired() =>
        Assert.IsFalse((await Validate(Token(new() { ["exp"] = EpochTime.GetIntDate(DateTime.UtcNow.AddMinutes(-3)) }))).IsValid);

    //an extreme exp must not overflow the replay-cache expiry
    [TestMethod]
    public async Task Valid_ExtremeExp_NoOverflow() =>
        Assert.IsTrue((await Validate(Token(new() { ["exp"] = 253402300799L }, jti: "jti-far"))).IsValid);

    [TestMethod]
    public async Task Invalid_ErrorNamesExceptionType_NotTokenContent()
    {
        var result = await Validate(Token(issuer: "https://evil.test"));
        Assert.IsFalse(result.IsValid);
        StringAssert.Contains(result.Error, "SecurityTokenInvalidIssuerException");
    }

    [TestMethod]
    public async Task Release_AllowsTheSameTokenAgain()
    {
        var token = Token(jti: "jti-rel");
        var first = await Validate(token);
        Assert.IsTrue(first.IsValid);
        Assert.IsFalse((await Validate(token)).IsValid);
        _validator.Release(first.ReplayKey);
        Assert.IsTrue((await Validate(token)).IsValid);
        _validator.Release(null);
    }

    [TestMethod]
    public async Task Invalid_Replay()
    {
        var token = Token(jti: "jti-1");
        Assert.IsTrue((await Validate(token)).IsValid);
        var replay = await Validate(token);
        Assert.IsFalse(replay.IsValid);
    }

    [TestMethod]
    public async Task Valid_NoJti() => Assert.IsTrue((await Validate(Token(new() { ["jti"] = null }))).IsValid);

    [TestMethod]
    public async Task Invalid_WrongIssuer() => Assert.IsFalse((await Validate(Token(issuer: "https://evil.test"))).IsValid);

    [TestMethod]
    public async Task Invalid_WrongAudience() => Assert.IsFalse((await Validate(Token(audience: "other"))).IsValid);

    [TestMethod]
    public async Task Invalid_UnknownKey() => Assert.IsFalse((await Validate(Token(key: OtherKey))).IsValid);

    //a symmetric token: the client secret is known to the client, so it must never sign a logout
    [TestMethod]
    public async Task Invalid_Symmetric()
    {
        var secret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('s', 32)));
        _configuration.SigningKeys.Add(secret);
        Assert.IsFalse((await Validate(Token(key: secret, algorithm: SecurityAlgorithms.HmacSha256))).IsValid);
    }

    [TestMethod]
    public async Task Invalid_OtherTyp() => Assert.IsFalse((await Validate(Token(typ: "at+jwt"))).IsValid);

    [TestMethod]
    public async Task Invalid_Garbage() => Assert.IsFalse((await Validate("not.a.token")).IsValid);

    [TestMethod]
    [DataRow(null, true)]
    [DataRow("", true)]
    [DataRow("logout+jwt", true)]
    [DataRow("LOGOUT+JWT", true)]
    [DataRow("jwt", true)]
    [DataRow("at+jwt", false)]
    public void TypeAccepted(string typ, bool expected) =>
        Assert.AreEqual(expected, OidcLogoutTokenValidator.TypeAccepted(typ));
}
