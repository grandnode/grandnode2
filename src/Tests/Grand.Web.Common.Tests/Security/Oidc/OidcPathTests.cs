using Grand.Web.Common.Security.Oidc;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.Security.Oidc;

[TestClass]
public class OidcPathTests
{
    [TestMethod]
    [DataRow("/api", "/api")]
    [DataRow("/api/", "/api")]
    [DataRow(" /api/v1/ ", "/api/v1")]
    public void Normalize_Valid(string input, string expected) => Assert.AreEqual(expected, OidcPath.Normalize(input));

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("/")]
    [DataRow("api")]
    [DataRow("//")]
    public void Normalize_Invalid(string input) => Assert.IsNull(OidcPath.Normalize(input));

    [TestMethod]
    [DataRow("/api")]
    [DataRow("/api/")]
    [DataRow("/API/Product/1")]
    public void Covers_True(string path) => Assert.IsTrue(OidcPath.Covers("/api", new PathString(path)));

    [TestMethod]
    [DataRow("/apix")]
    [DataRow("/apidocs/index")]
    [DataRow("/")]
    [DataRow("")]
    public void Covers_False(string path) => Assert.IsFalse(OidcPath.Covers("/api", new PathString(path)));
}
