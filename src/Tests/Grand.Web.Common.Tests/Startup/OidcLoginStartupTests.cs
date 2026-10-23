using Grand.Business.Core.Interfaces.Authentication;
using Grand.Web.Common.Security.Oidc;
using Grand.Web.Common.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Startup;

[TestClass]
public class OidcLoginStartupTests
{
    private static ServiceCollection ConfigureServices(string authority, string clientId)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["OidcResourceServer:Enabled"] = "true",
            ["OidcResourceServer:Authority"] = authority,
            ["OidcLogin:ClientId"] = clientId
        }).Build();
        var services = new ServiceCollection();
        new OidcLoginStartup().ConfigureServices(services, configuration);
        return services;
    }

    private static ServiceDescriptor Find(IServiceCollection services, Type serviceType) =>
        services.SingleOrDefault(x => x.ServiceType == serviceType);

    [TestMethod]
    public void ConfigureServices_Configured_RegistersOidcLoginServices()
    {
        var services = ConfigureServices("https://idp.test", "shop");

        Assert.AreEqual(typeof(OidcLoginProvider), Find(services, typeof(IExternalAuthenticationProvider))?.ImplementationType);
        Assert.AreEqual(typeof(OidcTokenRefresher), Find(services, typeof(IOidcTokenRefresher))?.ImplementationType);
        Assert.AreEqual(typeof(OidcSignOutProvider), Find(services, typeof(IExternalSignOutProvider))?.ImplementationType);
        Assert.AreEqual(ServiceLifetime.Scoped, Find(services, typeof(IExternalAuthenticationProvider)).Lifetime);
        Assert.AreEqual(ServiceLifetime.Singleton, Find(services, typeof(OidcLogoutTokenValidator))?.Lifetime);
        Assert.AreEqual(ServiceLifetime.Scoped, Find(services, typeof(OidcProfileSynchronizer))?.Lifetime);
    }

    [TestMethod]
    public void ConfigureServices_ClientIdEmpty_RegistersNoProvider()
    {
        AssertOnlyAlwaysRegisteredServices(ConfigureServices("https://idp.test", ""));
    }

    [TestMethod]
    public void ConfigureServices_ResourceServerAuthorityEmpty_RegistersNoProvider()
    {
        AssertOnlyAlwaysRegisteredServices(ConfigureServices("", "shop"));
    }

    //the controller and the back-channel endpoint are always mapped and resolve these
    private static void AssertOnlyAlwaysRegisteredServices(IServiceCollection services)
    {
        Assert.IsNull(Find(services, typeof(IExternalAuthenticationProvider)));
        Assert.IsNull(Find(services, typeof(IOidcTokenRefresher)));
        Assert.IsNull(Find(services, typeof(IExternalSignOutProvider)));
        Assert.IsNotNull(Find(services, typeof(OidcLogoutTokenValidator)));
        Assert.IsNotNull(Find(services, typeof(OidcProfileSynchronizer)));
    }
}
