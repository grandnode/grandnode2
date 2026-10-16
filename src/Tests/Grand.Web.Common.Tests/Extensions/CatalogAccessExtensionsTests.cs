using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Extensions;

[TestClass]
public class CatalogAccessExtensionsTests
{
    [TestMethod]
    public void CatalogAccess_TakesCustomerAndStoreOfTheRequest()
    {
        //Arrange
        var customer = new Customer();
        var accessor = new Mock<IContextAccessor>();
        accessor.Setup(a => a.WorkContext.CurrentCustomer).Returns(customer);
        accessor.Setup(a => a.StoreContext.CurrentStore).Returns(new Store { Id = "s1" });

        //Act
        var access = accessor.Object.CatalogAccess();

        //Assert
        Assert.AreSame(customer, access.Customer);
        Assert.AreEqual("s1", access.StoreId);
        Assert.IsFalse(access.ShowHidden);
    }
}
