using Grand.Business.Core.Extensions;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Extensions;

[TestClass]
public class MerchandiseReturnStatusChangedByTests
{
    [TestMethod]
    public void StatusChangedBy_Email()
    {
        Assert.AreEqual("john@shop.com", new Customer { Id = "c1", Email = "john@shop.com" }.StatusChangedBy());
    }

    [TestMethod]
    public void StatusChangedBy_NoEmail_FullName()
    {
        var staff = new Customer { Id = "c1" };
        staff.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.FirstName, Value = "John", StoreId = "" });
        staff.UserFields.Add(new UserField { Key = SystemCustomerFieldNames.LastName, Value = "Smith", StoreId = "" });

        Assert.AreEqual("John Smith", staff.StatusChangedBy());
    }

    [TestMethod]
    public void StatusChangedBy_NoEmailNoName_CustomerId()
    {
        Assert.AreEqual("staff (c1)", new Customer { Id = "c1" }.StatusChangedBy());
    }

    [TestMethod]
    public void StatusChangedBy_NoOne_Null()
    {
        Assert.IsNull(((Customer)null).StatusChangedBy());
    }
}
