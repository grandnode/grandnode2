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
        _selector = new OidcSchemeSelector(config, new OidcProtectedResourceRegistry(config, []));
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
    [TestMethod] public void IdpToken_Resource_Oidc() => Assert.AreEqual("Oidc", _selector.Select(Request("/api/Product", "Bearer " + Token(Issuer))));
    [TestMethod] public void LegacyToken_NoIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token(null))));
    [TestMethod] public void ForeignIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token("grandnode"))));
    [TestMethod] public void IdpToken_OutsideResources_Null() => Assert.IsNull(_selector.Select(Request("/customer/info", "Bearer " + Token(Issuer))));

    [TestMethod]
    public void AsymmetricToken_ForeignIssuer_Oidc()
    {
        var token = OidcTestHost.Token(issuer: "https://other.example.com");
        Assert.AreEqual("Oidc", _selector.Select(Request("/api/Product", "Bearer " + token)));
    }

    [TestMethod]
    public void HmacToken_ForeignIssuer_Null() => Assert.IsNull(_selector.Select(Request("/api/Product", "Bearer " + Token("https://other.example.com"))));

    [TestMethod]
    [DataRow("Bearer not-a-jwt")]
    [DataRow("Bearer ")]
    [DataRow("Basic dXNlcjpwYXNz")]
    public void Unreadable_TreatedAsNoToken(string header) => Assert.AreEqual("Oidc", _selector.Select(Request("/api/x", header)));

    [TestMethod]
    public void PostConfigure_SkipsOidc_ChainsPreviousSelector()
    {
        var oidc = new JwtBearerOptions();
        _selector.PostConfigure("Oidc", oidc);
        Assert.IsNull(oidc.ForwardDefaultSelector);

        var bearer = new JwtBearerOptions { ForwardDefaultSelector = _ => "Previous" };
        _selector.PostConfigure("Bearer", bearer);
        Assert.AreEqual("Oidc", bearer.ForwardDefaultSelector(Request("/api/x")));
        Assert.AreEqual("Previous", bearer.ForwardDefaultSelector(Request("/other")));
    }

    [TestMethod]
    [DataRow("https://idp.example.com/realms/shop", "https://idp.example.com/realms/shop/")]
    [DataRow("https://idp.example.com/realms/shop/", "https://idp.example.com/realms/shop")]
    public void TrailingSlash_Differences_StillOidc(string authority, string issuer)
    {
        var config = new OidcResourceServerConfig {
            Enabled = true, Authority = authority,
            Resources = [new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" }]
        };
        var selector = new OidcSchemeSelector(config, new OidcProtectedResourceRegistry(config, []));
        Assert.AreEqual("Oidc", selector.Select(Request("/api/Product", "Bearer " + Token(issuer))));
    }
}
