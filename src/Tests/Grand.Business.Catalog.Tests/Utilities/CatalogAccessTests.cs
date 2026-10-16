using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Utilities;

[TestClass]
public class CatalogAccessTests
{
    [TestMethod]
    public void For_WithoutCustomer_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => CatalogAccess.For(null, "s1"));
    }

    [TestMethod]
    public void For_LimitsToCustomerAndStore()
    {
        var customer = new Customer();

        var access = CatalogAccess.For(customer, "s1");

        Assert.AreSame(customer, access.Customer);
        Assert.AreEqual("s1", access.StoreId);
        Assert.IsFalse(access.ShowHidden);
    }

    [TestMethod]
    public void IncludingHidden_ShowsHiddenRecordsOfAnyStoreByDefault()
    {
        var access = CatalogAccess.IncludingHidden();

        Assert.IsNull(access.Customer);
        Assert.AreEqual("", access.StoreId);
        Assert.IsTrue(access.ShowHidden);
    }

    [TestMethod]
    public void IncludingHidden_TreatsNullStoreAsNoStore()
    {
        Assert.AreEqual(CatalogAccess.IncludingHidden(), CatalogAccess.IncludingHidden(null));
    }

    [TestMethod]
    public void Equals_ComparesCustomerAndStore()
    {
        var customer = new Customer();

        Assert.AreEqual(CatalogAccess.For(customer, "s1"), CatalogAccess.For(customer, "s1"));
        Assert.AreNotEqual(CatalogAccess.For(customer, "s1"), CatalogAccess.For(customer, "s2"));
        Assert.AreNotEqual(CatalogAccess.For(customer, "s1"), CatalogAccess.For(new Customer(), "s1"));
        Assert.AreNotEqual(CatalogAccess.IncludingHidden("s1"), CatalogAccess.IncludingHidden());
    }
}
