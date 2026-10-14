using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcLoginConfigTests
{
    [TestMethod]
    public void Defaults()
    {
        var config = new OidcLoginConfig();
        Assert.AreEqual("openid profile email offline_access", config.Scopes);
        Assert.AreEqual(TimeSpan.FromMinutes(30), config.StaffIdleTimeout);
        Assert.AreEqual(TimeSpan.FromHours(10), config.StaffMaxSession);
        Assert.AreEqual(TimeSpan.FromMinutes(10), config.RevalidationInterval);
        Assert.IsFalse(config.SsoOnly);
    }

    //a zero in appsettings must not mean "refresh at the provider on every request" or "end every session at once"
    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void NonPositiveValues_FallBackToDefaults(int value)
    {
        var config = new OidcLoginConfig {
            StaffIdleTimeoutMinutes = value, StaffMaxSessionHours = value, RevalidationMinutes = value
        };
        Assert.AreEqual(TimeSpan.FromMinutes(30), config.StaffIdleTimeout);
        Assert.AreEqual(TimeSpan.FromHours(10), config.StaffMaxSession);
        Assert.AreEqual(TimeSpan.FromMinutes(10), config.RevalidationInterval);
    }

    [TestMethod]
    [DataRow("https://idp", "client", true)]
    [DataRow("", "client", false)]
    [DataRow("https://idp", " ", false)]
    public void IsConfigured(string authority, string clientId, bool expected) =>
        Assert.AreEqual(expected, new OidcLoginConfig { ClientId = clientId }
            .IsConfigured(new OidcResourceServerConfig { Authority = authority }));

    [TestMethod]
    public void Defaults_MixedWithoutBreakGlass()
    {
        var config = new OidcLoginConfig();
        Assert.AreEqual(OidcSignInMode.Mixed, config.SignInMode);
        Assert.IsFalse(config.ProviderOnly);
        Assert.AreEqual(0, config.BreakGlassAccounts.Length);
    }

    [TestMethod]
    [DataRow("admin@shop.com", true)]
    [DataRow(" Admin@Shop.COM ", true)]
    [DataRow("other@shop.com", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsBreakGlass_TrimmedCaseInsensitive(string email, bool expected)
    {
        var config = new OidcLoginConfig { BreakGlassAccounts = [" ADMIN@shop.com", "", null] };
        Assert.AreEqual(expected, config.IsBreakGlass(email));
    }

    //an empty entry in appsettings must never make an account without e-mail a break-glass account
    [TestMethod]
    public void IsBreakGlass_NullList_False()
    {
        var config = new OidcLoginConfig { BreakGlassAccounts = null };
        Assert.IsFalse(config.IsBreakGlass("admin@shop.com"));
    }
}
