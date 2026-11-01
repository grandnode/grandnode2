using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Messages.Events;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Events;

[TestClass]
public class CustomerDeletedEventHandlerTests
{
    [TestMethod]
    public async Task Handle_DeletesTheInvoiceEmailsOfTheOrdersOfThatCustomer_DeletedOrdersToo()
    {
        var customer = new Customer();
        var order = new Order { CustomerId = customer.Id };
        var deletedOrder = new Order { CustomerId = customer.Id, Deleted = true };
        var otherOrder = new Order { CustomerId = "other" };
        var orderRepository = new Mock<IRepository<Order>>();
        orderRepository.Setup(c => c.Table).Returns(new[] { order, deletedOrder, otherOrder }.AsQueryable());
        orderRepository.Setup(c => c.ToListAsync(It.IsAny<IQueryable<string>>(), It.IsAny<CancellationToken>()))
            .Returns((IQueryable<string> query, CancellationToken _) => Task.FromResult<IList<string>>(query.ToList()));
        var queuedEmailService = new Mock<IQueuedEmailService>();
        var handler = new CustomerDeletedEventHandler(orderRepository.Object, queuedEmailService.Object);

        await handler.Handle(new EntityDeleted<Customer>(customer), CancellationToken.None);

        queuedEmailService.Verify(c => c.DeleteOrderInvoiceEmails(It.Is<IEnumerable<string>>(ids =>
            ids.Count() == 2 && ids.Contains(order.Id) && ids.Contains(deletedOrder.Id))), Times.Once);
    }
}
