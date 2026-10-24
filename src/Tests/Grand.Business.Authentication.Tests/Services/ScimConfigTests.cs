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

    //shopper accounts are taken over only with an e-mail of the company's own domains
    [TestMethod]
    [DataRow("jan@firma.pl", true)]
    [DataRow("Jan@FIRMA.PL", true)]
    [DataRow(" jan@firma.pl ", true)]
    [DataRow("jan@dzial.firma.pl", false)]
    [DataRow("jan@firma.pl.evil.com", false)]
    [DataRow("jan@gmail.com", false)]
    [DataRow("firma.pl", false)]
    [DataRow("", false)]
    [DataRow(null, false)]
    public void IsTakeOverDomain(string email, bool expected) =>
        Assert.AreEqual(expected, new ScimConfig { TakeOverDomains = ["firma.pl", " @Other.com "] }.IsTakeOverDomain(email));

    [TestMethod]
    public void IsTakeOverDomain_LeadingAtInConfig() =>
        Assert.IsTrue(new ScimConfig { TakeOverDomains = [" @Other.com "] }.IsTakeOverDomain("ola@other.com"));

    [TestMethod]
    public void IsTakeOverDomain_NoneConfigured_False()
    {
        Assert.IsFalse(new ScimConfig().IsTakeOverDomain("jan@firma.pl"));
        Assert.IsFalse(new ScimConfig { TakeOverDomains = null }.IsTakeOverDomain("jan@firma.pl"));
    }
}
