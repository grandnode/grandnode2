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