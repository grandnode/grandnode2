using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetRecurringPaymentsQueryHandlerTests
{
    private MongoDBRepositoryTest<Customer> _customerRepository;
    private GetRecurringPaymentsQueryHandler _handler;
    private MongoDBRepositoryTest<Order> _orderRepository;
    private MongoDBRepositoryTest<RecurringPayment> _repository;

    public GetRecurringPaymentsQueryHandlerTests()
    {
        _repository = new MongoDBRepositoryTest<RecurringPayment>();
        _customerRepository = new MongoDBRepositoryTest<Customer>();
        _orderRepository = new MongoDBRepositoryTest<Order>();
        _handler = new GetRecurringPaymentsQueryHandler(_repository, _customerRepository, _orderRepository);
    }

    [TestMethod]
    public async Task Handle_ByStore_ReturnsOnlyThatStore()
    {
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp1", StoreId = "s1" });
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp2", StoreId = "s2" });

        var result = await _handler.Handle(new GetRecurringPaymentsQuery { StoreId = "s1" }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("rp1", result[0].Id);
    }

    [TestMethod]
    public async Task Handle_ByStatus_ReturnsOnlyThatStatus()
    {
        await _repository.Collection.InsertOneAsync(new RecurringPayment
            { Id = "rp1", Status = RecurringPaymentStatus.PastDue });
        await _repository.Collection.InsertOneAsync(new RecurringPayment
            { Id = "rp2", Status = RecurringPaymentStatus.Active });

        var result = await _handler.Handle(
            new GetRecurringPaymentsQuery { Status = RecurringPaymentStatus.PastDue }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("rp1", result[0].Id);
    }

    [TestMethod]
    public async Task Handle_ByCustomerEmail_MatchesEveryAccountWithThatEmailExactly()
    {
        await _customerRepository.Collection.InsertOneAsync(new Customer { Id = "c1", Email = "jan@example.com", StoreId = "s1" });
        await _customerRepository.Collection.InsertOneAsync(new Customer { Id = "c2", Email = "jan@example.com", StoreId = "s2" });
        await _customerRepository.Collection.InsertOneAsync(new Customer { Id = "c3", Email = "xjan@example.com" });
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp1", CustomerId = "c1" });
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp2", CustomerId = "c2" });
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp3", CustomerId = "c3" });

        var result = await _handler.Handle(
            new GetRecurringPaymentsQuery { CustomerEmail = " Jan@Example.com " }, CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "rp1", "rp2" }, result.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Handle_ByUnknownCustomerEmail_ReturnsNothing()
    {
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp1", CustomerId = "c1" });

        var result = await _handler.Handle(
            new GetRecurringPaymentsQuery { CustomerEmail = "nobody@example.com" }, CancellationToken.None);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task Handle_BySalesEmployee_ReturnsOnlyPaymentsOfTheirInitialOrders()
    {
        await _orderRepository.Collection.InsertOneAsync(new Order { Id = "o1", SeId = "se1", RecurringCycleNumber = 1 });
        await _orderRepository.Collection.InsertOneAsync(new Order { Id = "o2", SeId = "se2", RecurringCycleNumber = 1 });
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp1", InitialOrderId = "o1" });
        await _repository.Collection.InsertOneAsync(new RecurringPayment { Id = "rp2", InitialOrderId = "o2" });

        var result = await _handler.Handle(
            new GetRecurringPaymentsQuery { SalesEmployeeId = "se1" }, CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("rp1", result[0].Id);
    }

    [TestMethod]
    public async Task Handle_ReturnsNewestFirst()
    {
        await _repository.Collection.InsertOneAsync(new RecurringPayment
            { Id = "old", CreatedOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        await _repository.Collection.InsertOneAsync(new RecurringPayment
            { Id = "new", CreatedOnUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc) });

        var result = await _handler.Handle(new GetRecurringPaymentsQuery(), CancellationToken.None);

        Assert.AreEqual("new", result[0].Id);
        Assert.AreEqual("old", result[1].Id);
    }
}
