using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Extensions;

[TestClass]
public class ProviderExtensionsTests
{
    [TestMethod]
    public void IsAuthenticateStoreTest_True()
    {
        var discountProvider = new DiscountProviderTest(["store1", "store2"], new List<string> { "group1" });
        Assert.IsTrue(discountProvider.IsAuthenticateStore(new Store { Id = "store1" }));
    }

    [TestMethod]
    public void IsAuthenticateStoreTest_True_NoStores()
    {
        var discountProvider = new DiscountProviderTest([], new List<string> { "group1" });
        Assert.IsTrue(discountProvider.IsAuthenticateStore(new Store { Id = "store1" }));
    }

    [TestMethod]
    public void IsAuthenticateStoreTest_False()
    {
        var discountProvider = new DiscountProviderTest(["store2"], new List<string> { "group1" });
        Assert.IsFalse(discountProvider.IsAuthenticateStore(new Store { Id = "store1" }));
    }

    [TestMethod]
    public void IsAuthenticateStoreTestId_True()
    {
        var discountProvider = new DiscountProviderTest(["store1", "store2"], new List<string> { "group1" });
        Assert.IsTrue(discountProvider.IsAuthenticateStore("store1"));
    }

    [TestMethod]
    public void IsAuthenticateStoreTestId_True_NoStores()
    {
        var discountProvider = new DiscountProviderTest([], new List<string> { "group1" });
        Assert.IsTrue(discountProvider.IsAuthenticateStore("store1"));
    }

    [TestMethod]
    public void IsAuthenticateStoreTestId_False()
    {
        var discountProvider = new DiscountProviderTest(["store2"], new List<string> { "group1" });
        Assert.IsFalse(discountProvider.IsAuthenticateStore("store1"));
    }

    [TestMethod]
    public void IsAuthenticateGroupTest_True()
    {
        var discountProvider = new DiscountProviderTest(["store2"], new List<string> { "group1" });
        var customer = new Customer();
        customer.Groups.Add("group1");
        Assert.IsTrue(discountProvider.IsAuthenticateGroup(customer));
    }

    [TestMethod]
    public void IsAuthenticateGroupTest_False()
    {
        var discountProvider = new DiscountProviderTest(["store2"], new List<string> { "group1" });
        var customer = new Customer();
        customer.Groups.Add("group2");
        Assert.IsFalse(discountProvider.IsAuthenticateGroup(customer));
    }
}