using Authentication.OpenIdConnect.Infrastructure;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Claims;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcLoginIdentityMapperTests
{
    private static ClaimsPrincipal P(params (string, string)[] claims) =>
        new(new ClaimsIdentity(claims.Select(c => new Claim(c.Item1, c.Item2)), "oidc"));

    [TestMethod]
    public void Maps_AllFields()
    {
        var id = OidcLoginIdentityMapper.Map(P(("sub", "s1"), ("email", "A@B.c"), ("email_verified", "True"),
            ("given_name", "Ann"), ("family_name", "Bell")), new OidcClaimNames());
        Assert.AreEqual("ExternalAuth.OpenIdConnect", id.ProviderSystemName);
        Assert.AreEqual("s1", id.Subject);
        Assert.AreEqual("A@B.c", id.Email);
        Assert.IsTrue(id.EmailVerified);
        Assert.AreEqual("Ann", id.FirstName);
        Assert.AreEqual("Bell", id.LastName);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("false")]
    [DataRow("yes")]
    public void EmailVerified_OnlyTrue(string value)
    {
        var claims = new List<(string, string)> { ("sub", "s1"), ("email", "a@b.c") };
        if (value != null) claims.Add(("email_verified", value));
        Assert.IsFalse(OidcLoginIdentityMapper.Map(P(claims.ToArray()), new OidcClaimNames()).EmailVerified);
    }

    [TestMethod]
    public void CustomClaimNames()
    {
        var names = new OidcClaimNames { Subject = "oid", Email = "upn", EmailVerified = "ev" };
        var id = OidcLoginIdentityMapper.Map(P(("oid", "o1"), ("upn", "u@b.c"), ("ev", "true")), names);
        Assert.AreEqual("o1", id.Subject);
        Assert.AreEqual("u@b.c", id.Email);
        Assert.IsTrue(id.EmailVerified);
    }

    [TestMethod]
    [DataRow(null, "a@b.c")]
    [DataRow("s1", null)]
    public void MissingSubjectOrEmail_Null(string sub, string email)
    {
        var claims = new List<(string, string)>();
        if (sub != null) claims.Add(("sub", sub));
        if (email != null) claims.Add(("email", email));
        Assert.IsNull(OidcLoginIdentityMapper.Map(P(claims.ToArray()), new OidcClaimNames()));
    }

    [TestMethod]
    public void PendingLifetime_IsAbsolute()
    {
        var now = DateTimeOffset.UtcNow;
        var props = new AuthenticationProperties { IsPersistent = true };
        OidcLoginIdentityMapper.ApplyPendingLifetime(props, now);
        Assert.IsFalse(props.IsPersistent);
        Assert.IsFalse(props.AllowRefresh);
        //the properties keep whole seconds
        Assert.IsTrue((now.AddMinutes(15) - props.ExpiresUtc.Value).Duration() < TimeSpan.FromSeconds(1));
    }
}
