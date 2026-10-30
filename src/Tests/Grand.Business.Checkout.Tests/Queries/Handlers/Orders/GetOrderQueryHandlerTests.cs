using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetOrderQueryHandlerTests
{
    private GetOrderQueryHandler _handler;
    private MongoDBRepositoryTest<Order> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Order>();
        _handler = new GetOrderQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Handle_FiltersByCustomerAndPages()
    {
        //Arrange - written to the collection directly, InsertAsync overwrites CreatedOnUtc
        var now = DateTime.UtcNow;
        await _repository.Collection.InsertOneAsync(new Order { CustomerId = "c1", CreatedOnUtc = now.AddDays(-3) });
        await _repository.Collection.InsertOneAsync(new Order { CustomerId = "c1", CreatedOnUtc = now.AddDays(-1), Code = "NEWEST" });
        await _repository.Collection.InsertOneAsync(new Order { CustomerId = "c1", CreatedOnUtc = now.AddDays(-2) });
        await _repository.Collection.InsertOneAsync(new Order { CustomerId = "c2", CreatedOnUtc = now });
        await _repository.Collection.InsertOneAsync(new Order { CustomerId = "c1", CreatedOnUtc = now, Deleted = true });

        //Act
        var result = await _handler.Handle(new GetOrderQuery { CustomerId = "c1", PageSize = 2 },
            CancellationToken.None);

        //Assert
        Assert.AreEqual(3, result.TotalCount);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("NEWEST", result[0].Code);
    }

    [TestMethod]
    public async Task Handle_FiltersByPurchaseOrderNumber()
    {
        await _repository.Collection.InsertOneAsync(new Order { PurchaseOrderNumber = "PO-1", Code = "A" });
        await _repository.Collection.InsertOneAsync(new Order { PurchaseOrderNumber = "PO-10", Code = "B" });
        await _repository.Collection.InsertOneAsync(new Order { Code = "C" });

        var result = await _handler.Handle(new GetOrderQuery { PurchaseOrderNumber = " PO-1 " },
            CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("A", result[0].Code);
    }

    [TestMethod]
    public async Task Handle_OverdueOnly()
    {
        var now = DateTime.UtcNow;
        await _repository.Collection.InsertOneAsync(new Order {
            Code = "OVERDUE", PaymentDueDateUtc = now.AddDays(-1), PaymentStatusId = PaymentStatus.Pending
        });
        await _repository.Collection.InsertOneAsync(new Order {
            Code = "PAID", PaymentDueDateUtc = now.AddDays(-1), PaymentStatusId = PaymentStatus.Paid
        });
        await _repository.Collection.InsertOneAsync(new Order {
            Code = "CANCELLED", PaymentDueDateUtc = now.AddDays(-1), PaymentStatusId = PaymentStatus.Pending,
            OrderStatusId = (int)OrderStatusSystem.Cancelled
        });
        await _repository.Collection.InsertOneAsync(new Order {
            Code = "NOTDUE", PaymentDueDateUtc = now.AddDays(1), PaymentStatusId = PaymentStatus.Pending
        });
        await _repository.Collection.InsertOneAsync(new Order { Code = "NOCREDIT", PaymentStatusId = PaymentStatus.Pending });

        var result = await _handler.Handle(new GetOrderQuery { OverdueOnly = true }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("OVERDUE", result[0].Code);
    }

    [TestMethod]
    public async Task Handle_OverdueOnly_DueTodayIsNotListed()
    {
        await _repository.Collection.InsertOneAsync(new Order {
            Code = "DUETODAY", PaymentDueDateUtc = DateTime.UtcNow.Date, PaymentStatusId = PaymentStatus.Pending
        });

        var result = await _handler.Handle(new GetOrderQuery { OverdueOnly = true }, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }
}
