using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Customers;

[TestClass]
public class CustomerExtensionsTests
{
    [TestMethod]
    public void GetCustomerGroupIds_ReturnsTheGroupsSorted()
    {
        var customer = new Customer();
        customer.Groups.Add("registered");
        customer.Groups.Add("Members");
        customer.Groups.Add("guests");

        var ids = customer.GetCustomerGroupIds();

        CollectionAssert.AreEqual(new[] { "Members", "guests", "registered" }, ids);
    }

    [TestMethod]
    public void GetCustomerGroupIds_DoesNotReorderTheCustomerGroups()
    {
        var customer = new Customer();
        customer.Groups.Add("registered");
        customer.Groups.Add("guests");

        customer.GetCustomerGroupIds();

        CollectionAssert.AreEqual(new[] { "registered", "guests" }, customer.Groups.ToArray());
    }

    [TestMethod]
    public void GetCustomerGroupsCacheKey_JoinsTheSortedGroups()
    {
        var customer = new Customer();
        customer.Groups.Add("registered");
        customer.Groups.Add("guests");

        Assert.AreEqual("guests,registered", customer.GetCustomerGroupsCacheKey());
    }

    [TestMethod]
    public void GetCustomerGroupsCacheKey_WithoutGroups_IsEmpty()
    {
        Assert.AreEqual("", new Customer().GetCustomerGroupsCacheKey());
    }
}
