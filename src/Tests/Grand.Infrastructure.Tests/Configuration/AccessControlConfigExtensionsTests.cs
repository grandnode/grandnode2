using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Configuration;

[TestClass]
public class AccessControlConfigExtensionsTests
{
    private static Customer CustomerInGroups()
    {
        var customer = new Customer();
        customer.Groups.Add("registered");
        customer.Groups.Add("guests");
        return customer;
    }

    [TestMethod]
    public void GetCustomerGroupsCacheKey_AclOn_JoinsTheSortedGroups()
    {
        var config = new AccessControlConfig();

        Assert.AreEqual("guests,registered", config.GetCustomerGroupsCacheKey(CustomerInGroups()));
    }

    [TestMethod]
    public void GetCustomerGroupsCacheKey_AclIgnored_IsEmpty()
    {
        var config = new AccessControlConfig { IgnoreAcl = true };

        Assert.AreEqual("", config.GetCustomerGroupsCacheKey(CustomerInGroups()));
    }
}
