using Authentication.OpenIdConnect;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcLoginProviderTests
{
    [TestMethod]
    public async Task PanelSignInPath_WithScheme()
    {
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        schemes.Setup(x => x.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme)).ReturnsAsync(
            new AuthenticationScheme(OidcLoginDefaults.AuthenticationScheme, null, typeof(IAuthenticationHandler)));
        var provider = new OidcLoginProvider(new OidcLoginSettings(), schemes.Object);
        Assert.AreEqual("/oidc/login?returnUrl=%2Fadmin", await provider.GetPanelSignInPath("/admin"));
    }

    [TestMethod]
    public async Task PanelSignInPath_NoScheme_Null()
    {
        var provider = new OidcLoginProvider(new OidcLoginSettings(), new Mock<IAuthenticationSchemeProvider>().Object);
        Assert.IsNull(await provider.GetPanelSignInPath("/admin"));
    }

    [TestMethod]
    public void SystemName_IsTheLinkName() =>
        Assert.AreEqual(OidcResourceServerDefaults.ProviderSystemName, new OidcLoginProvider(new OidcLoginSettings(), new Mock<IAuthenticationSchemeProvider>().Object).SystemName);

    [TestMethod]
    public void Priority_FromSettings() =>
        Assert.AreEqual(7, new OidcLoginProvider(new OidcLoginSettings { DisplayOrder = 7 }, new Mock<IAuthenticationSchemeProvider>().Object).Priority);

    [TestMethod]
    [DataRow("https://idp", "client", true)]
    [DataRow("", "client", false)]
    [DataRow("https://idp", " ", false)]
    public void IsConfigured(string authority, string clientId, bool expected) =>
        Assert.AreEqual(expected, new OidcLoginConfig { ClientId = clientId }
            .IsConfigured(new OidcResourceServerConfig { Authority = authority }));
}
