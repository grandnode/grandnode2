using Grand.Business.Authentication.Services;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class OidcSignInModeCheckTests
{
    private readonly OidcResourceServerConfig _resourceServer = new() { Authority = "https://idp" };

    [TestMethod]
    public void Mixed_NeverAProblem() =>
        Assert.IsNull(OidcSignInModeCheck.Problem(new OidcLoginConfig(), new OidcResourceServerConfig()));

    [TestMethod]
    public void ProviderOnly_Working_NoProblem() =>
        Assert.IsNull(OidcSignInModeCheck.Problem(
            new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly, ClientId = "shop" }, _resourceServer));

    [TestMethod]
    public void ProviderOnly_NotConfigured_Problem() =>
        Assert.IsNotNull(OidcSignInModeCheck.Problem(
            new OidcLoginConfig { SignInMode = OidcSignInMode.ProviderOnly }, _resourceServer));
}
