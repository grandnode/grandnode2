using Grand.Business.Checkout.Events.Orders;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Events.Orders;

[TestClass]
public class RecurringPaymentMessagesHandlerTests
{
    private Customer _customer;
    private RecurringPaymentMessagesHandler _handler;
    private Mock<IRecurringPaymentMessageProviderService> _messages;
    private Order _order;
    private RecurringPayment _recurringPayment;

    [TestInitialize]
    public void Init()
    {
        var orderService = new Mock<IOrderService>();
        var customerService = new Mock<ICustomerService>();
        _messages = new Mock<IRecurringPaymentMessageProviderService>();
        _handler = new RecurringPaymentMessagesHandler(orderService.Object, customerService.Object,
            _messages.Object, new LanguageSettings { DefaultAdminLanguageId = "admin-lang" },
            new Mock<ILogger<RecurringPaymentMessagesHandler>>().Object);
        _order = new Order { Id = "o1", CustomerLanguageId = "customer-lang" };
        _customer = new Customer { Id = "c1" };
        _recurringPayment = new RecurringPayment { Id = "r1", InitialOrderId = "o1", CustomerId = "c1" };
        orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order);
        customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(_customer);
    }

    [TestMethod]
    public async Task Cancelled_SendsBothCancelledMessages()
    {
        await _handler.Handle(new RecurringPaymentCancelledEvent(_recurringPayment), default);

        _messages.Verify(x => x.SendRecurringPaymentCancelledCustomerMessage(_recurringPayment, _order,
            _customer, "customer-lang"), Times.Once);
        _messages.Verify(x => x.SendRecurringPaymentCancelledStoreOwnerMessage(_recurringPayment, _order,
            _customer, "admin-lang"), Times.Once);
        _messages.Verify(x => x.SendRecurringPaymentFailedCustomerMessage(It.IsAny<RecurringPayment>(),
            It.IsAny<Order>(), It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
        _messages.Verify(x => x.SendRecurringPaymentFailedStoreOwnerMessage(It.IsAny<RecurringPayment>(),
            It.IsAny<Order>(), It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PastDue_SendsBothFailedMessages()
    {
        await _handler.Handle(new RecurringPaymentPastDueEvent(_recurringPayment), default);

        _messages.Verify(x => x.SendRecurringPaymentFailedCustomerMessage(_recurringPayment, _order,
            _customer, "customer-lang"), Times.Once);
        _messages.Verify(x => x.SendRecurringPaymentFailedStoreOwnerMessage(_recurringPayment, _order,
            _customer, "admin-lang"), Times.Once);
        _messages.Verify(x => x.SendRecurringPaymentCancelledCustomerMessage(It.IsAny<RecurringPayment>(),
            It.IsAny<Order>(), It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
        _messages.Verify(x => x.SendRecurringPaymentCancelledStoreOwnerMessage(It.IsAny<RecurringPayment>(),
            It.IsAny<Order>(), It.IsAny<Customer>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task CustomerMessageThrows_StoreOwnerStillGetsItsMessage_AndNothingThrows()
    {
        _messages.Setup(x => x.SendRecurringPaymentCancelledCustomerMessage(It.IsAny<RecurringPayment>(),
            It.IsAny<Order>(), It.IsAny<Customer>(), It.IsAny<string>())).ThrowsAsync(new Exception("smtp"));

        await _handler.Handle(new RecurringPaymentCancelledEvent(_recurringPayment), default);

        _messages.Verify(x => x.SendRecurringPaymentCancelledStoreOwnerMessage(_recurringPayment, _order,
            _customer, "admin-lang"), Times.Once);
    }

    [TestMethod]
    public async Task InitialOrderMissing_SendsNothing()
    {
        _recurringPayment.InitialOrderId = "gone";

        await _handler.Handle(new RecurringPaymentCancelledEvent(_recurringPayment), default);
        await _handler.Handle(new RecurringPaymentPastDueEvent(_recurringPayment), default);

        _messages.VerifyNoOtherCalls();
    }
}
