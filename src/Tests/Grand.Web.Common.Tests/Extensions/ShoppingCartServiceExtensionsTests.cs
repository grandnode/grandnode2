using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Extensions;

[TestClass]
public class ShoppingCartServiceExtensionsTests
{
    [TestMethod]
    public async Task GetCurrentCart_ReadsTheCartOfTheRequestCustomerInTheRequestStore()
    {
        //Arrange
        var customer = new Customer();
        var accessor = new Mock<IContextAccessor>();
        accessor.Setup(a => a.WorkContext.CurrentCustomer).Returns(customer);
        accessor.Setup(a => a.StoreContext.CurrentStore).Returns(new Store { Id = "s1" });
        IList<ShoppingCartItem> items = [new ShoppingCartItem()];
        var service = new Mock<IShoppingCartService>();
        service.Setup(s => s.GetShoppingCart(customer, "s1", ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions))
            .ReturnsAsync(items);

        //Act
        var result = await service.Object.GetCurrentCart(accessor.Object, ShoppingCartType.ShoppingCart,
            ShoppingCartType.Auctions);

        //Assert
        Assert.AreSame(items, result);
    }
}
