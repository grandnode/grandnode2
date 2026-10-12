using Authentication.OpenIdConnect.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Claims;
using System.Text;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcLoginSessionDataTests
{
    private static string IdToken(params Claim[] claims) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('k', 32))), SecurityAlgorithms.HmacSha256)
        });

    [TestMethod]
    public void Capture_RefreshTokenAndSid()
    {
        var properties = new AuthenticationProperties();
        OidcLoginSessionData.Capture(properties, new OpenIdConnectMessage {
            RefreshToken = "rt-1", IdToken = IdToken(new Claim("sub", "s"), new Claim("sid", "sid-1"))
        });
        Assert.AreEqual("rt-1", properties.Items[OidcLoginSessionData.RefreshTokenItem]);
        Assert.AreEqual("sid-1", properties.Items[OidcLoginSessionData.SidItem]);
    }

    [TestMethod]
    public void Capture_NoRefreshTokenNoSid_NothingStored()
    {
        var properties = new AuthenticationProperties();
        OidcLoginSessionData.Capture(properties, new OpenIdConnectMessage { IdToken = IdToken(new Claim("sub", "s")) });
        Assert.IsFalse(properties.Items.ContainsKey(OidcLoginSessionData.RefreshTokenItem));
        Assert.IsFalse(properties.Items.ContainsKey(OidcLoginSessionData.SidItem));
    }

    [TestMethod]
    public void Capture_NullResponse_NoThrow() =>
        OidcLoginSessionData.Capture(new AuthenticationProperties(), null);

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not-a-jwt")]
    public void Sid_Unreadable_Null(string idToken) => Assert.IsNull(OidcLoginSessionData.Sid(idToken));

    //sid is optional: a value of another type must never fail the sign-in callback
    [TestMethod]
    public void Sid_NonStringValue_NoThrow() =>
        OidcLoginSessionData.Sid(IdToken(new Claim("sub", "s"), new Claim("sid", "5", ClaimValueTypes.Integer)));
}
