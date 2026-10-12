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
        Assert.IsFalse(config.StaffSsoOnly);
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
}
