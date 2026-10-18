using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
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
}
