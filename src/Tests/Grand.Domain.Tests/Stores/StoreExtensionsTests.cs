using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Stores;

[TestClass]
public class StoreExtensionsTests
{
    private Store store;

    [TestInitialize]
    public void Setup()
    {
        store = new Store();
        store.Domains.Add(new DomainHost { HostName = "google.com", Url = "https:\\google.com" });
        store.Domains.Add(new DomainHost { HostName = "yahoo.com", Url = "https:\\yahoo.com" });
    }

    [TestMethod]
    public void ContainsHostValueTest_True()
    {
        Assert.IsTrue(store.ContainsHostValue("Google.com"));
    }

    [TestMethod]
    public void ContainsHostValueTest_False()
    {
        Assert.IsFalse(store.ContainsHostValue("google"));
    }

    [TestMethod]
    public void PrimaryHost_ReturnsThePrimaryDomain()
    {
        store.Domains[1].Primary = true;

        Assert.AreEqual("yahoo.com", store.PrimaryHost().HostName);
    }

    [TestMethod]
    public void PrimaryHost_WithoutAPrimaryDomain_ReturnsTheFirstDomain()
    {
        Assert.AreEqual("google.com", store.PrimaryHost().HostName);
    }

    [TestMethod]
    public void PrimaryHost_WithoutDomains_FallsBackToTheStoreUrl()
    {
        var withoutDomains = new Store { Url = "https://shop.example/", Domains = null };

        Assert.AreEqual("https://shop.example/", withoutDomains.PrimaryHost().Url);
    }

    [TestMethod]
    public void LinkHost_ReturnsTheRequestHostWhenItIsOneOfTheStoreDomains()
    {
        store.Domains[0].Primary = true;

        Assert.AreEqual("yahoo.com", store.LinkHost(new DomainHost { HostName = "Yahoo.com" }).HostName);
    }

    [TestMethod]
    public void LinkHost_ForeignOrMissingHost_ReturnsThePrimaryDomain()
    {
        store.Domains[1].Primary = true;

        Assert.AreEqual("yahoo.com", store.LinkHost(new DomainHost { HostName = "other.example" }).HostName);
        Assert.AreEqual("yahoo.com", store.LinkHost(null).HostName);
    }

    [TestMethod]
    public void HostValueTest_NotNull()
    {
        Assert.IsNotNull(store.HostValue("yAhoO.com"));
    }

    [TestMethod]
    public void HostValueTest_Null()
    {
        Assert.IsNull(store.HostValue("yahoo"));
    }
}