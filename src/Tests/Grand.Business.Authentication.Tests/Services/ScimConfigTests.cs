using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class ScimConfigTests
{
    [TestMethod]
    public void Defaults()
    {
        var config = new ScimConfig();
        Assert.IsTrue(config.ExternalIdIsSubject);
        Assert.AreEqual(100, config.MaxResults);
        Assert.IsFalse(config.TokenUsable);
    }

    //a short secret is guessable: the endpoint must refuse every request rather than accept it
    [TestMethod]
    [DataRow(null, false)]
    [DataRow("", false)]
    [DataRow("0123456789012345678901234567890", false)]
    [DataRow("01234567890123456789012345678901", true)]
    public void TokenUsable(string token, bool expected) =>
        Assert.AreEqual(expected, new ScimConfig { Token = token }.TokenUsable);

    [TestMethod]
    [DataRow(null, 100)]
    [DataRow(500, 100)]
    [DataRow(-1, 0)]
    [DataRow(0, 0)]
    [DataRow(10, 10)]
    public void PageSize(int? requested, int expected) =>
        Assert.AreEqual(expected, new ScimConfig().PageSize(requested));

    [TestMethod]
    [DataRow(0)]
    [DataRow(-3)]
    public void NonPositiveMaxResults_FallsBackToDefault(int value) =>
        Assert.AreEqual(100, new ScimConfig { MaxResults = value }.PageSize(null));
}
