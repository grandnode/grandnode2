using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class OrderPlacedEventHandlerTests
{
    private Mock<ICustomerService> _customerServiceMock;
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private OrderPlacedEventHandler _orderPlacedEventHandler;
    private Mock<IRepository<ProductAlsoPurchased>> _productAlsoPurchasedRepository;

    [TestInitialize]
    public void Init()
    {
        _productAlsoPurchasedRepository = new Mock<IRepository<ProductAlsoPurchased>>();
        _customerServiceMock = new Mock<ICustomerService>();
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _orderPlacedEventHandler = new OrderPlacedEventHandler(_productAlsoPurchasedRepository.Object,
            _customerServiceMock.Object, _loyaltyPointsServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var order = new Order { RedeemedLoyaltyPointsAmount = 10 };
        order.OrderItems.Add(new OrderItem { ProductId = "1" });
        order.OrderItems.Add(new OrderItem { ProductId = "2" });
        var notification = new OrderPlacedEvent(order);
        //Act
        await _orderPlacedEventHandler.Handle(notification, CancellationToken.None);
        //Assert
        _productAlsoPurchasedRepository.Verify(c => c.InsertAsync(It.IsAny<ProductAlsoPurchased>()), Times.AtLeastOnce);
        _loyaltyPointsServiceMock.Verify(
            c => c.AddLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<double>()), Times.Once);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(order.CustomerId, x => x.FreeShipping, false),
            Times.Once);
        _customerServiceMock.Verify(
            c => c.UpdateCustomerField(order.CustomerId, x => x.LastPurchaseDateUtc, order.CreatedOnUtc), Times.Once);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(order.CustomerId, x => x.LastUpdateCartDateUtc, null),
            Times.Once);
        _customerServiceMock.Verify(c => c.UpdateCustomerField(order.CustomerId, x => x.HasContributions, true),
            Times.Once);
    }
}