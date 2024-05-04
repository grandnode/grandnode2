using Grand.Business.Checkout.Events.ShoppingCart;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.ShoppingCart;

[TestClass]
public class ShoppingCartItemDeletedEventHandlerTests
{
    private Mock<IProductReservationService> _productReservationServiceMock;
    private ShoppingCartItemDeletedEventHandler _shoppingCartItemDeletedEventHandler;

    [TestInitialize]
    public void Init()
    {
        _productReservationServiceMock = new Mock<IProductReservationService>();
        _shoppingCartItemDeletedEventHandler =
            new ShoppingCartItemDeletedEventHandler(_productReservationServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        _productReservationServiceMock.Setup(x => x.GetCustomerReservationsHelperBySciId(It.IsAny<string>())).Returns(
            Task.FromResult((IList<CustomerReservationsHelper>)new List<CustomerReservationsHelper> { new() }));
        var notification = new EntityDeleted<ShoppingCartItem>(new ShoppingCartItem
            { RentalStartDateUtc = DateTime.UtcNow, RentalEndDateUtc = DateTime.UtcNow });
        //Act
        await _shoppingCartItemDeletedEventHandler.Handle(notification, CancellationToken.None);
        //Assert
        _productReservationServiceMock.Verify(
            c => c.DeleteCustomerReservationsHelper(It.IsAny<CustomerReservationsHelper>()), Times.Once);
    }
}