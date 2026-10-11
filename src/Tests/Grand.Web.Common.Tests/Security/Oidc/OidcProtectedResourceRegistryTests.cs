using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcProtectedResourceRegistryTests
{
    private class Provider(params OidcProtectedResource[] resources) : IOidcProtectedResourceProvider
    {
        public IEnumerable<OidcProtectedResource> GetResources() => resources;
    }

    private class ThrowingProvider : IOidcProtectedResourceProvider
    {
        public IEnumerable<OidcProtectedResource> GetResources() => throw new InvalidOperationException("boom");
    }

    private static OidcResourceServerConfig Config(params OidcProtectedResource[] resources) =>
        new() { Enabled = true, Authority = "https://idp.example.com", Resources = [.. resources] };

    [TestMethod]
    public void MergesConfigAndProviders_AndMatches()
    {
        var registry = new OidcProtectedResourceRegistry(
            Config(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" }),
            [new Provider(new OidcProtectedResource { Name = "Tools", PathPrefix = "/tools/" })]);

        Assert.AreEqual(2, registry.Resources.Count);
        Assert.AreEqual(0, registry.Errors.Count);
        Assert.AreEqual("BackendApi", registry.Match(new PathString("/api/Product")).Name);
        Assert.AreEqual("Tools", registry.Match(new PathString("/TOOLS")).Name);
        Assert.IsNull(registry.Match(new PathString("/apix")));
    }

    [TestMethod]
    public void Resources_BuiltOnce_ReadOnly()
    {
        var registry = new OidcProtectedResourceRegistry(
            Config(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" }), []);

        Assert.AreSame(registry.Resources, registry.Resources);
        Assert.IsNotInstanceOfType(registry.Resources, typeof(List<OidcProtectedResource>));
    }

    [TestMethod]
    public void Disabled_NoResources()
    {
        var config = Config(new OidcProtectedResource { Name = "BackendApi", PathPrefix = "/api" });
        config.Enabled = false;
        var registry = new OidcProtectedResourceRegistry(config, []);

        Assert.AreEqual(0, registry.Resources.Count);
        Assert.IsNull(registry.Match(new PathString("/api")));
    }

    [TestMethod]
    public void DuplicateName_IgnoringCase_IsAnError_SecondDropped()
    {
        var registry = new OidcProtectedResourceRegistry(
            Config(new OidcProtectedResource { Name = "Api", PathPrefix = "/a" }),
            [new Provider(new OidcProtectedResource { Name = "API", PathPrefix = "/b" })]);

        Assert.AreEqual(1, registry.Resources.Count);
        Assert.AreEqual(1, registry.Errors.Count);
    }

    [TestMethod]
    [DataRow("/api", "/API/")]
    [DataRow("/api", "/api/v2")]
    [DataRow("/api/v2", "/api")]
    public void DuplicateOrNestedPrefix_IsAnError(string first, string second)
    {
        var registry = new OidcProtectedResourceRegistry(
            Config(new OidcProtectedResource { Name = "One", PathPrefix = first },
                new OidcProtectedResource { Name = "Two", PathPrefix = second }), []);

        Assert.AreEqual(1, registry.Resources.Count);
        Assert.AreEqual(1, registry.Errors.Count);
    }

    [TestMethod]
    [DataRow(null, "/x")]
    [DataRow(" ", "/x")]
    [DataRow("Name", "/")]
    [DataRow("Name", "x")]
    public void InvalidNameOrPrefix_IsAnError(string name, string prefix)
    {
        var registry = new OidcProtectedResourceRegistry(
            Config(new OidcProtectedResource { Name = name, PathPrefix = prefix }), []);

        Assert.AreEqual(0, registry.Resources.Count);
        Assert.AreEqual(1, registry.Errors.Count);
    }

    [TestMethod]
    public void ProviderThrows_IsAnError_NotACrash()
    {
        var registry = new OidcProtectedResourceRegistry(Config(), [new ThrowingProvider()]);
        Assert.AreEqual(1, registry.Errors.Count);
    }
}
