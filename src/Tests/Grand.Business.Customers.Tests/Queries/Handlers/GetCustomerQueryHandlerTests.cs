using Grand.Business.Core.Queries.Customers;
using Grand.Business.Customers.Queries.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Customers.Tests.Queries.Handlers;

[TestClass]
public class GetCustomerQueryHandlerTests
{
    private GetCustomerQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        var _repository = new MongoDBRepositoryTest<Customer>();
        _repository.Insert(new Customer());
        _repository.Insert(new Customer());
        _repository.Insert(new Customer());
        _repository.Insert(new Customer());
        handler = new GetCustomerQueryHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Assert
        var customerQuery = new GetCustomerQuery();
        //Act
        var result = await handler.Handle(customerQuery, CancellationToken.None);

        //Assert
        Assert.AreEqual(4, result.Count());
    }
}