using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcLoginProviderTests
{
    [TestMethod]
    public async Task PanelSignInPath_WithScheme()
    {
        var schemes = new Mock<IAuthenticationSchemeProvider>();
        schemes.Setup(x => x.GetSchemeAsync(OidcLoginDefaults.AuthenticationScheme)).ReturnsAsync(
            new AuthenticationScheme(OidcLoginDefaults.AuthenticationScheme, null, typeof(IAuthenticationHandler)));
        var provider = new OidcLoginProvider(new OidcLoginConfig(), schemes.Object);
        Assert.AreEqual("/oidc/login?returnUrl=%2Fadmin", await provider.GetSignInPath("/admin"));
    }

    [TestMethod]
    public async Task PanelSignInPath_NoScheme_Null()
    {
        var provider = new OidcLoginProvider(new OidcLoginConfig(), new Mock<IAuthenticationSchemeProvider>().Object);
        Assert.IsNull(await provider.GetSignInPath("/admin"));
    }

    [TestMethod]
    public void SystemName_IsTheLinkName() =>
        Assert.AreEqual(OidcResourceServerDefaults.ProviderSystemName, new OidcLoginProvider(new OidcLoginConfig(), new Mock<IAuthenticationSchemeProvider>().Object).SystemName);

    //the grid's configure link opens the read-only card below the grid, not the same page again
    [TestMethod]
    public void ConfigurationUrl_PointsAtTheCard() =>
        Assert.AreEqual("../ExternalAuthentication/Methods#oidc-sign-in",
            new OidcLoginProvider(new OidcLoginConfig(), new Mock<IAuthenticationSchemeProvider>().Object).ConfigurationUrl);

    [TestMethod]
    public void Priority_FromSettings() =>
        Assert.AreEqual(7, new OidcLoginProvider(new OidcLoginConfig { DisplayOrder = 7 }, new Mock<IAuthenticationSchemeProvider>().Object).Priority);

    [TestMethod]
    [DataRow("https://idp", "client", true)]
    [DataRow("", "client", false)]
    [DataRow("https://idp", " ", false)]
    public void IsConfigured(string authority, string clientId, bool expected) =>
        Assert.AreEqual(expected, new OidcLoginConfig { ClientId = clientId }
            .IsConfigured(new OidcResourceServerConfig { Authority = authority }));
}
