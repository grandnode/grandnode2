using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcSchemeSelectorTests
{
    private const string Issuer = "https://idp.example.com/realms/shop";
    private OidcSchemeSelector _selector;

    [TestInitialize]
    public void Init()
    {
        var config = new OidcResourceServerConfig {
            Enabled = true, Authority = Issuer + "/",
            Resources = [new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" }]
        };
        _selector = new OidcSchemeSelector(new OidcProtectedResourceRegistry(config, []));
    }

    private static string Token(string issuer) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
        Issuer = issuer,
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(new string('k', 32))), SecurityAlgorithms.HmacSha256)
    });

    private static HttpContext Request(string path, string authorization = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (authorization != null) context.Request.Headers.Authorization = authorization;
        return context;
    }

    [TestMethod] public void NoToken_Resource_Oidc() => Assert.AreEqual("Oidc", _selector.Select(Request("/api/Product")));
    //routing reads the algorithm only: an HMAC token stays with Bearer whatever issuer it claims (Oidc accepts no HMAC)
    [TestMethod] public void HmacToken_IdpIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token(Issuer))));
    [TestMethod] public void LegacyToken_NoIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token(null))));
    [TestMethod] public void ForeignIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token("grandnode"))));
    [TestMethod] public void IdpToken_OutsideResources_Null() => Assert.IsNull(_selector.Select(Request("/customer/info", "Bearer " + OidcTestHost.Token())));

    [TestMethod]
    public void AsymmetricToken_ForeignIssuer_Oidc()
    {
        var token = OidcTestHost.Token(issuer: "https://other.example.com");
        Assert.AreEqual("Oidc", _selector.Select(Request("/api/Product", "Bearer " + token)));
    }

    [TestMethod]
    public void HmacToken_ForeignIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token("https://other.example.com"))));

    private static string WithAlgorithm(string algorithm)
    {
        static string B64(string json) => Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(json));
        return B64("{\"alg\":\"" + algorithm + "\",\"typ\":\"JWT\"}") + "." + B64("{\"sub\":\"1\"}") + ".c2ln";
    }

    [TestMethod]
    [DataRow("RS256")]
    [DataRow("PS384")]
    [DataRow("ES512")]
    public void AcceptedAsymmetricAlgorithm_Oidc(string algorithm) =>
        Assert.AreEqual("Oidc", _selector.Select(Request("/api/Product", "Bearer " + WithAlgorithm(algorithm))));

    //the Oidc scheme's own list decides, not a prefix: these start with RS / ES but Oidc never accepts them
    [TestMethod]
    [DataRow("RSA-OAEP")]
    [DataRow("ES256K")]
    [DataRow("HS256")]
    [DataRow("none")]
    public void AlgorithmOutsideTheOidcList_Null(string algorithm) =>
        Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + WithAlgorithm(algorithm))));

    [TestMethod]
    [DataRow("Bearer not-a-jwt")]
    [DataRow("Bearer x.y.z")]
    [DataRow("Bearer ")]
    [DataRow("Basic dXNlcjpwYXNz")]
    public void Unreadable_TreatedAsNoToken(string header) => Assert.AreEqual("Oidc", _selector.Select(Request("/api/x", header)));

    [TestMethod]
    public void DecidedOncePerRequest()
    {
        var context = Request("/api/Product", "Bearer " + OidcTestHost.Token());
        Assert.AreEqual("Oidc", _selector.Select(context));
        Assert.IsTrue(context.Items.ContainsKey(OidcResourceServerDefaults.RouteItemKey));

        //the token is not read again: the cached decision answers
        context.Request.Headers.Authorization = "Bearer " + Token(null);
        Assert.AreEqual("Oidc", _selector.Select(context));
    }

    [TestMethod]
    public void PathChanged_DecidedAgain()
    {
        var context = Request("/api/Product", "Bearer " + OidcTestHost.Token());
        Assert.AreEqual("Oidc", _selector.Select(context));

        context.Request.Path = "/other";
        Assert.IsNull(_selector.Select(context));
    }

    [TestMethod]
    public void OversizedToken_TreatedAsNoToken()
    {
        var header = "Bearer " + WithAlgorithm("HS256") + new string('a', TokenValidationParameters.DefaultMaximumTokenSizeInBytes);
        Assert.AreEqual("Oidc", _selector.Select(Request("/api/Product", header)));
    }

    [TestMethod]
    public void PostConfigure_Bearer_ChainsPreviousSelector()
    {
        var bearer = new JwtBearerOptions { ForwardDefaultSelector = _ => "Previous" };
        _selector.PostConfigure("Bearer", bearer);
        Assert.AreEqual("Oidc", bearer.ForwardDefaultSelector(Request("/api/x")));
        Assert.AreEqual("Previous", bearer.ForwardDefaultSelector(Request("/other")));
    }

    [TestMethod]
    [DataRow("Oidc")]
    [DataRow("FrontAuthentication")]
    [DataRow("PluginJwt")]
    public void PostConfigure_OtherJwtBearerScheme_KeepsItsOwnSelector(string scheme)
    {
        Func<HttpContext, string> own = _ => null;
        var options = new JwtBearerOptions { ForwardDefaultSelector = own };
        _selector.PostConfigure(scheme, options);
        Assert.AreSame(own, options.ForwardDefaultSelector);

        var none = new JwtBearerOptions();
        _selector.PostConfigure(scheme, none);
        Assert.IsNull(none.ForwardDefaultSelector);
    }
}
