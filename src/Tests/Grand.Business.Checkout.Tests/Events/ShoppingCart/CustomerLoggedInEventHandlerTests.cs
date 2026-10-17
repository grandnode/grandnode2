using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.ShoppingCart;

[TestClass]
public class CustomerLoggedInEventHandlerTests
{
    private CustomerLoggedInEventHandler _customerLoggedInEventHandler;
    private Mock<IShoppingCartService> _shoppingCartServiceMock;

    [TestInitialize]
    public void Init()
    {
        _shoppingCartServiceMock = new Mock<IShoppingCartService>();

        _customerLoggedInEventHandler = new CustomerLoggedInEventHandler(_shoppingCartServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var notification = new CustomerLoggedInEvent(new Customer(), new Customer(), "", new Language());
        //Act
        await _customerLoggedInEventHandler.Handle(notification, CancellationToken.None);
        //Assert
        _shoppingCartServiceMock.Verify(c => c.MigrateShoppingCart(It.IsAny<Customer>(), It.IsAny<Customer>(),
                It.IsAny<string>(), It.IsAny<Language>(), true),
            Times.Once);
    }

    [TestMethod]
    public async Task Handle_MigratesFromThePreviousCustomerInTheEventStoreAndLanguage()
    {
        //Arrange
        var guest = new Customer();
        var customer = new Customer();
        var language = new Language { Id = "lang-b" };

        //Act
        await _customerLoggedInEventHandler.Handle(new CustomerLoggedInEvent(customer, guest, "store-b", language),
            CancellationToken.None);

        //Assert
        _shoppingCartServiceMock.Verify(c => c.MigrateShoppingCart(guest, customer, "store-b", language, true),
            Times.Once);
    }
}