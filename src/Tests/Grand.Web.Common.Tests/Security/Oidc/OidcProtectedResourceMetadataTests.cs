using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcProtectedResourceMetadataTests
{
    private readonly OidcResourceServerConfig _config = new() { Enabled = true, Authority = "https://idp.example.com/realms/shop/" };

    private readonly List<OidcProtectedResource> _resources = [
        new() { Name = "BackendApi", PathPrefix = "/api", ScopesSupported = ["grandnode.api", "offline_access"] },
        new() { Name = "Tools", PathPrefix = "/tools", Resource = "https://tools.example.com/", ScopesSupported = ["tools.read"] }
    ];

    private const string BaseUrl = "https://shop.example.com/eu";

    private JsonElement Json(OidcProtectedResource resource) =>
        JsonSerializer.SerializeToElement(OidcProtectedResourceMetadata.Build(_config, _resources, resource, BaseUrl));

    private static string[] Strings(JsonElement e, string name) =>
        e.GetProperty(name).EnumerateArray().Select(x => x.GetString()).ToArray();

    [TestMethod]
    public void Resource_Document()
    {
        var json = Json(_resources[0]);
        Assert.AreEqual("https://shop.example.com/eu/api", json.GetProperty("resource").GetString());
        CollectionAssert.AreEqual(new[] { "https://idp.example.com/realms/shop/" }, Strings(json, "authorization_servers"));
        CollectionAssert.AreEqual(new[] { "grandnode.api" }, Strings(json, "scopes_supported"));
        CollectionAssert.AreEqual(new[] { "header" }, Strings(json, "bearer_methods_supported"));
        Assert.AreEqual("BackendApi", json.GetProperty("resource_name").GetString());
    }

    [TestMethod]
    public void ConfiguredResourceUri_IsCanonical() =>
        Assert.AreEqual("https://tools.example.com", Json(_resources[1]).GetProperty("resource").GetString());

    [TestMethod]
    public void Root_Document_BaseUrlAndScopeUnion()
    {
        var json = Json(null);
        Assert.AreEqual("https://shop.example.com/eu", json.GetProperty("resource").GetString());
        CollectionAssert.AreEquivalent(new[] { "grandnode.api", "tools.read" }, Strings(json, "scopes_supported"));
        Assert.IsFalse(json.TryGetProperty("resource_name", out _));
    }

    [TestMethod]
    [DataRow("https://shop.example.com/api", "https://shop.example.com/.well-known/oauth-protected-resource/api")]
    [DataRow("https://shop.example.com", "https://shop.example.com/.well-known/oauth-protected-resource")]
    [DataRow("https://api.example.com/v1", "https://api.example.com/.well-known/oauth-protected-resource/v1")]
    [DataRow("https://shop.example.com:8443/eu/api", "https://shop.example.com:8443/.well-known/oauth-protected-resource/eu/api")]
    [DataRow("grandnode-api", null)]
    [DataRow(null, null)]
    public void MetadataUrl_WellKnownBetweenHostAndPath(string resourceUri, string expected) =>
        Assert.AreEqual(expected, OidcProtectedResourceMetadata.MetadataUrl(resourceUri));

    [TestMethod]
    public void MetadataUrl_StoreUnderSubPath_AtHostRoot() =>
        Assert.AreEqual("https://shop.example.com/.well-known/oauth-protected-resource/eu/api",
            OidcProtectedResourceMetadata.MetadataUrl(OidcCanonicalUri.ResourceFor(_resources[0], BaseUrl)));

    [TestMethod]
    public void MetadataUrl_ConfiguredResource() =>
        Assert.AreEqual("https://tools.example.com/.well-known/oauth-protected-resource",
            OidcProtectedResourceMetadata.MetadataUrl(OidcCanonicalUri.ResourceFor(_resources[1], BaseUrl)));
}
