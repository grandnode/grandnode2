using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetPaymentTransactionQueryHandlerTests
{
    private GetPaymentTransactionQueryHandler _handler;
    private MongoDBRepositoryTest<Order> _orderRepository;
    private MongoDBRepositoryTest<PaymentTransaction> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<PaymentTransaction>();
        _orderRepository = new MongoDBRepositoryTest<Order>();
        _handler = new GetPaymentTransactionQueryHandler(_repository, _orderRepository);
    }

    [TestMethod]
    public async Task Handle_WithoutTemp_LeavesOutAbandonedCheckouts()
    {
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderCode = "ORDER", Temp = false });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderCode = "ABANDONED", Temp = true });

        var result = await _handler.Handle(new GetPaymentTransactionQuery { IncludeTemp = false }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ORDER", result[0].OrderCode);
    }

    [TestMethod]
    public async Task Handle_ByDefault_IncludesAbandonedCheckouts()
    {
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderCode = "ORDER", Temp = false });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderCode = "ABANDONED", Temp = true });

        var result = await _handler.Handle(new GetPaymentTransactionQuery(), CancellationToken.None);

        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task Handle_ByPaymentMethod_ReturnsOnlyThatMethod()
    {
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { PaymentMethodSystemName = "Payments.OnAccount" });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { PaymentMethodSystemName = "Payments.CashOnDelivery" });

        var result = await _handler.Handle(
            new GetPaymentTransactionQuery { PaymentMethodSystemName = "Payments.OnAccount" }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Payments.OnAccount", result[0].PaymentMethodSystemName);
    }

    [TestMethod]
    public async Task Handle_BySalesEmployee_OnlyTheirOrders()
    {
        var theirs = Guid.NewGuid();
        var others = Guid.NewGuid();
        await _orderRepository.Collection.InsertOneAsync(new Order { OrderGuid = theirs, SeId = "se-1" });
        await _orderRepository.Collection.InsertOneAsync(new Order { OrderGuid = others, SeId = "se-2" });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderGuid = theirs });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderGuid = others });

        var result = await _handler.Handle(new GetPaymentTransactionQuery { SalesEmployeeId = "se-1" },
            CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(theirs, result[0].OrderGuid);
    }

    [TestMethod]
    public async Task Handle_ByPurchaseOrderNumber_MatchesTrimmedOrderInStore()
    {
        var inStore = Guid.NewGuid();
        var otherStore = Guid.NewGuid();
        await _orderRepository.Collection.InsertOneAsync(new Order { OrderGuid = inStore, StoreId = "s1", PurchaseOrderNumber = "PO-7" });
        await _orderRepository.Collection.InsertOneAsync(new Order { OrderGuid = otherStore, StoreId = "s2", PurchaseOrderNumber = "PO-7" });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderGuid = inStore, StoreId = "s1" });
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderGuid = otherStore, StoreId = "s2" });

        var result = await _handler.Handle(
            new GetPaymentTransactionQuery { StoreId = "s1", PurchaseOrderNumber = " PO-7 " }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(inStore, result[0].OrderGuid);
    }

    [TestMethod]
    public async Task Handle_OverdueOnly_LeavesOutDueTodayAndPaid()
    {
        var today = DateTime.UtcNow.Date;
        var overdue = Guid.NewGuid();
        var dueToday = Guid.NewGuid();
        var paid = Guid.NewGuid();
        await _orderRepository.Collection.InsertOneAsync(new Order {
            OrderGuid = overdue, PaymentDueDateUtc = today.AddDays(-1), PaymentStatusId = PaymentStatus.Pending,
            OrderStatusId = (int)OrderStatusSystem.Pending
        });
        await _orderRepository.Collection.InsertOneAsync(new Order {
            OrderGuid = dueToday, PaymentDueDateUtc = today, PaymentStatusId = PaymentStatus.Pending,
            OrderStatusId = (int)OrderStatusSystem.Pending
        });
        await _orderRepository.Collection.InsertOneAsync(new Order {
            OrderGuid = paid, PaymentDueDateUtc = today.AddDays(-1), PaymentStatusId = PaymentStatus.Paid,
            OrderStatusId = (int)OrderStatusSystem.Pending
        });
        foreach (var orderGuid in new[] { overdue, dueToday, paid })
            await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderGuid = orderGuid });

        var result = await _handler.Handle(new GetPaymentTransactionQuery { OverdueOnly = true }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(overdue, result[0].OrderGuid);
    }

    [TestMethod]
    public async Task Handle_OrderCriteriaMatchingNoOrder_ReturnsNothing()
    {
        await _repository.Collection.InsertOneAsync(new PaymentTransaction { OrderGuid = Guid.NewGuid() });

        var result = await _handler.Handle(new GetPaymentTransactionQuery { PurchaseOrderNumber = "NONE" },
            CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }
}
