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
}
