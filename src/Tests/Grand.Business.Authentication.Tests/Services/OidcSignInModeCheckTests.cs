using Grand.Business.Authentication.Services;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcSignInModeCheckTests
{
    private readonly OidcResourceServerConfig _resourceServer = new() { Authority = "https://idp" };

    private static PluginInfo[] Plugin(bool installed) =>
        [new PluginInfo { SystemName = OidcCustomerResolver.ProviderSystemName, Installed = installed }];

    [TestMethod]
    public void Mixed_NeverAProblem() =>
        Assert.IsNull(OidcSignInModeCheck.Problem(new OidcLoginConfig(), new OidcResourceServerConfig(), []));

    [TestMethod]
    public void ProviderOnly_Working_NoProblem() =>
        Assert.IsNull(OidcSignInModeCheck.Problem(
            new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly, ClientId = "shop" }, _resourceServer, Plugin(true)));

    [TestMethod]
    public void ProviderOnly_NotConfigured_Problem() =>
        Assert.IsNotNull(OidcSignInModeCheck.Problem(
            new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly }, _resourceServer, Plugin(true)));

    [TestMethod]
    public void ProviderOnly_PluginNotInstalled_Problem()
    {
        var config = new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly, ClientId = "shop" };
        Assert.IsNotNull(OidcSignInModeCheck.Problem(config, _resourceServer, Plugin(false)));
        Assert.IsNotNull(OidcSignInModeCheck.Problem(config, _resourceServer, null));
    }
}
