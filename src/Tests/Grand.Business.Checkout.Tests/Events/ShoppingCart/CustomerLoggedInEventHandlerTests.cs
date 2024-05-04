using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.ShoppingCart;

[TestClass]
public class CustomerLoggedInEventHandlerTests
{
    private CustomerLoggedInEventHandler _customerLoggedInEventHandler;
    private Mock<IShoppingCartService> _shoppingCartServiceMock;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void Init()
    {
        _shoppingCartServiceMock = new Mock<IShoppingCartService>();
        _workContextMock = new Mock<IWorkContext>();
        _customerLoggedInEventHandler =
            new CustomerLoggedInEventHandler(_shoppingCartServiceMock.Object, _workContextMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var notification = new CustomerLoggedInEvent(new Customer());
        //Act
        await _customerLoggedInEventHandler.Handle(notification, CancellationToken.None);
        //Assert
        _shoppingCartServiceMock.Verify(c => c.MigrateShoppingCart(It.IsAny<Customer>(), It.IsAny<Customer>(), true),
            Times.Once);
    }
}