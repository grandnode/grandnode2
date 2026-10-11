using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Claims;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcTokenChecksTests
{
    private static ClaimsPrincipal With(params Claim[] claims) => new(new ClaimsIdentity(claims, "Oidc"));

    [TestMethod]
    public void Audience_MatchesCanonicalResource() =>
        Assert.IsTrue(OidcTokenChecks.HasAudience(
            With(new Claim("aud", "account"), new Claim("aud", "https://Shop.example.com/api/")),
            "https://shop.example.com/api", []));

    [TestMethod]
    public void Audience_MatchesConfiguredValue() =>
        Assert.IsTrue(OidcTokenChecks.HasAudience(With(new Claim("aud", "318274561")), "https://shop.example.com/api", ["318274561"]));

    [TestMethod]
    public void Audience_MissingOrOther()
    {
        Assert.IsFalse(OidcTokenChecks.HasAudience(With(), "https://shop.example.com/api", []));
        Assert.IsFalse(OidcTokenChecks.HasAudience(With(new Claim("aud", "https://shop.example.com/mcp")), "https://shop.example.com/api", ["x"]));
    }

    [TestMethod]
    public void AuthorizedParty_EmptyListAllowsAll() => Assert.IsTrue(OidcTokenChecks.HasAuthorizedParty(With(), []));

    [TestMethod]
    public void AuthorizedParty_AzpThenClientId()
    {
        Assert.IsTrue(OidcTokenChecks.HasAuthorizedParty(With(new Claim("azp", "cli")), ["cli"]));
        Assert.IsTrue(OidcTokenChecks.HasAuthorizedParty(With(new Claim("client_id", "cli")), ["cli"]));
        Assert.IsFalse(OidcTokenChecks.HasAuthorizedParty(With(new Claim("azp", "other"), new Claim("client_id", "cli")), ["cli"]));
        Assert.IsFalse(OidcTokenChecks.HasAuthorizedParty(With(), ["cli"]));
    }

    [TestMethod]
    public void Scopes_SpaceSeparated_AndScpVariants()
    {
        Assert.AreEqual(0, OidcTokenChecks.MissingScopes(With(new Claim("scope", "openid  grandnode.api")), ["grandnode.api"]).Count);
        Assert.AreEqual(0, OidcTokenChecks.MissingScopes(With(new Claim("scp", "a"), new Claim("scp", "b")), ["a", "b"]).Count);
        Assert.AreEqual(0, OidcTokenChecks.MissingScopes(With(new Claim("scp", "a b")), ["b"]).Count);
    }

    [TestMethod]
    public void Scopes_MissingReported_CaseSensitive_NullRequiredIsEmpty()
    {
        CollectionAssert.AreEqual(new[] { "grandnode.api" },
            OidcTokenChecks.MissingScopes(With(new Claim("scope", "GrandNode.Api")), ["grandnode.api"]).ToArray());
        Assert.AreEqual(0, OidcTokenChecks.MissingScopes(With(), null).Count);
    }

    [TestMethod]
    public void PublicScopes_DropsOfflineAccessBlanksDuplicates() =>
        CollectionAssert.AreEqual(new[] { "a", "b" }, OidcTokenChecks.PublicScopes(["a", "offline_access", " ", "b", "a"]));
}
