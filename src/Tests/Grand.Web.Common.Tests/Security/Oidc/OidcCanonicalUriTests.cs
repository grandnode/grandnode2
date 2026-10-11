using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Oidc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcCanonicalUriTests
{
    [TestMethod]
    public void Derived_FromBaseUrlAndPrefix() =>
        Assert.AreEqual("https://shop.example.com/eu/api",
            OidcCanonicalUri.ResourceFor(new OidcProtectedResource { PathPrefix = "/api/" }, "https://shop.example.com/eu"));

    [TestMethod]
    public void Derived_KeepsNonDefaultPort_LowercasesHost() =>
        Assert.AreEqual("https://shop.example.com:8443/api",
            OidcCanonicalUri.ResourceFor(new OidcProtectedResource { PathPrefix = "/api" }, "https://Shop.Example.com:8443"));

    [TestMethod]
    public void Configured_WinsAndIsNormalized() =>
        Assert.AreEqual("https://api.example.com/v1",
            OidcCanonicalUri.ResourceFor(new OidcProtectedResource { PathPrefix = "/api", Resource = "HTTPS://API.Example.com:443/v1/" }, "https://shop.example.com"));

    [TestMethod]
    public void Derived_NoBaseUrl_Null() =>
        Assert.IsNull(OidcCanonicalUri.ResourceFor(new OidcProtectedResource { PathPrefix = "/api" }, null));

    [TestMethod]
    [DataRow("https://shop.example.com/api", "https://shop.example.com/api")]
    [DataRow("https://Shop.Example.com/api/", "https://shop.example.com/api")]
    [DataRow("HTTPS://shop.example.com:443/api", "https://shop.example.com/api")]
    [DataRow("grandnode-api", "grandnode-api")]
    public void Equal_True(string a, string b) => Assert.IsTrue(OidcCanonicalUri.Equal(a, b));

    [TestMethod]
    [DataRow("https://shop.example.com/api2", "https://shop.example.com/api")]
    [DataRow("https://shop.example.com/API", "https://shop.example.com/api")]
    [DataRow("http://shop.example.com/api", "https://shop.example.com/api")]
    [DataRow("https://shop.example.com:8443/api", "https://shop.example.com/api")]
    [DataRow("Grandnode-api", "grandnode-api")]
    [DataRow(null, "grandnode-api")]
    public void Equal_False(string a, string b) => Assert.IsFalse(OidcCanonicalUri.Equal(a, b));
}
