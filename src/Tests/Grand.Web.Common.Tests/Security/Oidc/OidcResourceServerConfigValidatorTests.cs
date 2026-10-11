using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcResourceServerConfigValidatorTests
{
    private static OidcResourceServerConfig Valid() => new() {
        Enabled = true,
        Authority = "https://idp.example.com/realms/shop",
        Resources = [new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api", Resource = "https://shop.example.com/api" }]
    };

    private static IList<string> Validate(OidcResourceServerConfig config, bool dev = false) =>
        OidcResourceServerConfigValidator.Validate(config, new OidcProtectedResourceRegistry(config, []), dev);

    [TestMethod]
    public void Disabled_NoErrors() => Assert.AreEqual(0, Validate(new OidcResourceServerConfig()).Count);

    [TestMethod]
    public void Valid_NoErrors() => Assert.AreEqual(0, Validate(Valid()).Count);

    [TestMethod]
    [DataRow("")]
    [DataRow("not-a-url")]
    [DataRow("ftp://idp.example.com")]
    public void BadAuthority(string authority)
    {
        var config = Valid();
        config.Authority = authority;
        Assert.AreEqual(1, Validate(config).Count);
    }

    [TestMethod]
    public void HttpAuthority_OnlyInDevelopment()
    {
        var config = Valid();
        config.Authority = "http://localhost:8080/realms/shop";
        Assert.AreEqual(2, Validate(config).Count);
        config.RequireHttpsMetadata = false;
        Assert.AreEqual(1, Validate(config).Count);
        Assert.AreEqual(0, Validate(config, true).Count);
    }

    [TestMethod]
    public void HttpAuthority_RequiresHttpsMetadataOff()
    {
        var config = Valid();
        config.Authority = "http://localhost:8080/realms/shop";
        var errors = Validate(config, true);
        Assert.AreEqual(1, errors.Count);
        Assert.AreEqual("OidcResourceServer.RequireHttpsMetadata must be false for an http Authority.", errors[0]);
    }

    [TestMethod]
    public void NoResources()
    {
        var config = Valid();
        config.Resources = [];
        Assert.AreEqual(1, Validate(config).Count);
    }

    [TestMethod]
    public void OnlyWithdrawnResources_NoError()
    {
        //the Web API is switched off: its resource is configured but withdrawn, which is not a misconfiguration
        var config = Valid();
        var registry = new OidcProtectedResourceRegistry(config, [], [new DisabledApi()]);
        Assert.AreEqual(0, registry.Resources.Count);
        Assert.AreEqual(0, OidcResourceServerConfigValidator.Validate(config, registry, false).Count);
    }

    private class DisabledApi : Grand.Business.Core.Interfaces.Authentication.IOidcDisabledPathProvider
    {
        public IEnumerable<string> GetDisabledPaths() => ["/api"];
    }

    [TestMethod]
    public void RegistryErrorsIncluded()
    {
        var config = Valid();
        config.Resources.Add(new OidcProtectedResource { Name = "Nested", PathPrefix = "/api/v2" });
        Assert.AreEqual(1, Validate(config).Count);
    }

    [TestMethod]
    [DataRow("shop.example.com/api")]
    [DataRow("https://shop.example.com/api#x")]
    public void BadResourceUri(string resource)
    {
        var config = Valid();
        config.Resources[0].Resource = resource;
        Assert.AreEqual(1, Validate(config).Count);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(301)]
    public void ClockSkewOutOfRange(int seconds)
    {
        var config = Valid();
        config.ClockSkewSeconds = seconds;
        Assert.AreEqual(1, Validate(config).Count);
    }

    [TestMethod]
    public void EmptyClaimName()
    {
        var config = Valid();
        config.Claims.Subject = "";
        Assert.AreEqual(1, Validate(config).Count);
    }
}
