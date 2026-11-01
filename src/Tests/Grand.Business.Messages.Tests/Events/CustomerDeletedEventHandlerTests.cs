using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.Events;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Events;

[TestClass]
public class CustomerDeletedEventHandlerTests
{
    [TestMethod]
    public async Task Handle_DeletesTheInvoiceEmailsOfTheOrdersOfThatCustomer()
    {
        var customer = new Customer();
        var order = new Order { CustomerId = customer.Id };
        var orderService = new Mock<IOrderService>();
        orderService.Setup(c => c.SearchOrders(It.IsAny<string>(), It.IsAny<string>(), customer.Id, It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(),
                It.IsAny<PaymentStatus?>(), It.IsAny<ShippingStatus?>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Order>(new List<Order> { order }, 0, int.MaxValue));
        var queuedEmailService = new Mock<IQueuedEmailService>();
        var handler = new CustomerDeletedEventHandler(orderService.Object, queuedEmailService.Object);

        await handler.Handle(new EntityDeleted<Customer>(customer), CancellationToken.None);

        queuedEmailService.Verify(c => c.DeleteOrderInvoiceEmails(
            It.Is<IEnumerable<string>>(ids => ids.Single() == order.Id)), Times.Once);
    }
}
