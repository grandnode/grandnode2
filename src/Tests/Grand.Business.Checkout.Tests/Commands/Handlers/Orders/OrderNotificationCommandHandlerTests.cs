using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Messages.Common;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Domain.Orders;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class OrderNotificationCommandHandlerTests
{
    private OrderNotificationCommandHandler _handler;
    private Mock<IOrderMessageProviderService> _orderMessageProviderServiceMock;
    private Mock<IOrderService> _orderServiceMock;

    [TestInitialize]
    public void Init()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _orderMessageProviderServiceMock = new Mock<IOrderMessageProviderService>();
        _handler = new OrderNotificationCommandHandler(_orderServiceMock.Object, _orderMessageProviderServiceMock.Object,
            new OrderSettings(), new LanguageSettings(), new Mock<IPdfService>().Object,
            new Mock<ILogger<OrderNotificationCommandHandler>>().Object, new Mock<IMediator>().Object);
    }

    [TestMethod]
    public async Task Handle_AddsImpersonationNoteForTheOriginalCustomer()
    {
        //Arrange
        var staff = new Customer { Email = "staff@shop.example" };

        //Act
        await _handler.Handle(new OrderNotificationCommand {
            Order = new Order { Id = "order" }, Customer = new Customer(), OriginalCustomerIfImpersonated = staff
        }, CancellationToken.None);

        //Assert
        _orderServiceMock.Verify(x => x.InsertOrderNote(It.Is<OrderNote>(n =>
            n.OrderId == "order" && n.Note.Contains("staff@shop.example") && n.Note.Contains("impersonating"))), Times.Once);
    }

    [TestMethod]
    public async Task Handle_ByDefault_NotifiesStoreOwnerAndVendors()
    {
        var customer = new Customer();
        var order = new Order { Id = "order", CustomerLanguageId = "en" };

        await _handler.Handle(new OrderNotificationCommand { Order = order, Customer = customer }, CancellationToken.None);

        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedStoreOwnerMessage(order, customer,
            It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutStoreOwnerAndVendors_SendsOnlyTheCustomerMessage()
    {
        var customer = new Customer();
        var order = new Order { Id = "order", CustomerLanguageId = "en" };
        order.OrderItems.Add(new OrderItem { VendorId = "vendor" });

        await _handler.Handle(new OrderNotificationCommand {
            Order = order, Customer = customer, NotifyStoreOwnerAndVendors = false
        }, CancellationToken.None);

        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedStoreOwnerMessage(It.IsAny<Order>(),
            It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedVendorMessage(It.IsAny<Order>(),
            It.IsAny<Customer>(), It.IsAny<Vendor>(), It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Never);
        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedCustomerMessage(order, customer, "en",
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_WithoutCustomerNotification_SendsOnlyTheStoreOwnerMessage()
    {
        var customer = new Customer();
        var order = new Order { Id = "order", CustomerLanguageId = "en" };

        await _handler.Handle(new OrderNotificationCommand {
            Order = order, Customer = customer, NotifyCustomer = false
        }, CancellationToken.None);

        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedCustomerMessage(It.IsAny<Order>(),
            It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<DomainHost>()), Times.Never);
        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedStoreOwnerMessage(order, customer,
            It.IsAny<string>(), It.IsAny<DomainHost>()), Times.Once);
    }

    [TestMethod]
    public async Task Handle_SendsTheCustomerMessageForTheCommandCustomer()
    {
        //Arrange
        var customer = new Customer();
        var order = new Order { Id = "order", CustomerLanguageId = "en" };

        //Act
        await _handler.Handle(new OrderNotificationCommand { Order = order, Customer = customer }, CancellationToken.None);

        //Assert
        _orderMessageProviderServiceMock.Verify(x => x.SendOrderPlacedCustomerMessage(order, customer, "en",
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Once);
    }
}
