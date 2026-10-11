using Authentication.OpenIdConnect;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcLoginProviderTests
{
    [TestMethod]
    public void SystemName_IsTheLinkName() =>
        Assert.AreEqual(OidcResourceServerDefaults.ProviderSystemName, new OidcLoginProvider(new OidcLoginSettings()).SystemName);

    [TestMethod]
    public void Priority_FromSettings() =>
        Assert.AreEqual(7, new OidcLoginProvider(new OidcLoginSettings { DisplayOrder = 7 }).Priority);

    [TestMethod]
    [DataRow("https://idp", "client", true)]
    [DataRow("", "client", false)]
    [DataRow("https://idp", " ", false)]
    public void IsConfigured(string authority, string clientId, bool expected) =>
        Assert.AreEqual(expected, new OidcLoginConfig { ClientId = clientId }
            .IsConfigured(new OidcResourceServerConfig { Authority = authority }));
}
