using Authentication.OpenIdConnect;
using Authentication.OpenIdConnect.Infrastructure;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Plugins.OpenIdConnect;

[TestClass]
public class OidcSignOutProviderTests
{
    private const string Back = "https://shop.test/signed-out";

    private Mock<IExternalAuthenticationService> _authenticationService;
    private Mock<IConfigurationManager<OpenIdConnectConfiguration>> _manager;
    private Mock<ILogger<OidcSignOutProvider>> _logger;

    [TestInitialize]
    public void Init()
    {
        _authenticationService = new Mock<IExternalAuthenticationService>();
        _authenticationService.Setup(x => x.AuthenticationProviderIsAvailable(OidcLoginDefaults.ProviderSystemName))
            .Returns(true);
        _manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        _logger = new Mock<ILogger<OidcSignOutProvider>>();
        Metadata("https://idp.test/logout");
    }

    private void Metadata(string endSession) =>
        _manager.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OpenIdConnectConfiguration { EndSessionEndpoint = endSession });

    private OidcSignOutProvider Provider(bool withManager = true)
    {
        var monitor = new Mock<IOptionsMonitor<OpenIdConnectOptions>>();
        monitor.Setup(x => x.Get(OidcLoginDefaults.AuthenticationScheme)).Returns(new OpenIdConnectOptions {
            ConfigurationManager = withManager ? _manager.Object : null
        });
        return new OidcSignOutProvider(monitor.Object, new OidcLoginConfig { ClientId = "shop app" },
            _authenticationService.Object, _logger.Object);
    }

    [TestMethod]
    public async Task EndpointPresent_BuildsUrl()
    {
        var url = await Provider().GetSignOutUrl(Back);
        Assert.AreEqual("https://idp.test/logout?client_id=shop%20app&post_logout_redirect_uri=" +
                        Uri.EscapeDataString(Back), url);
    }

    [TestMethod]
    public async Task EndpointWithQuery_AppendsWithAmpersand()
    {
        Metadata("https://idp.test/logout?realm=a");
        var url = await Provider().GetSignOutUrl(Back);
        Assert.IsTrue(url.StartsWith("https://idp.test/logout?realm=a&client_id=shop%20app&"), url);
    }

    [TestMethod]
    public async Task NoEndpoint_Null()
    {
        Metadata(null);
        Assert.IsNull(await Provider().GetSignOutUrl(Back));
    }

    [TestMethod]
    public async Task NoConfigurationManager_Null() =>
        Assert.IsNull(await Provider(false).GetSignOutUrl(Back));

    [TestMethod]
    public async Task NotAvailable_Null()
    {
        _authenticationService.Setup(x => x.AuthenticationProviderIsAvailable(OidcLoginDefaults.ProviderSystemName))
            .Returns(false);
        Assert.IsNull(await Provider().GetSignOutUrl(Back));
        _manager.Verify(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task MetadataThrows_NullAndWarning()
    {
        _manager.Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("down"));
        Assert.IsNull(await Provider().GetSignOutUrl(Back));
        _logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }
}
