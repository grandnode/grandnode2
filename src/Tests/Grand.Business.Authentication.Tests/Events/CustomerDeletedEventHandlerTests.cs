using Grand.Business.Authentication.Events;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Events;

[TestClass]
public class CustomerDeletedEventHandlerTests
{
    private CustomerDeletedEventHandler _handler;
    private IRepository<ExternalAuthentication> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<ExternalAuthentication>();
        _handler = new CustomerDeletedEventHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        await _repository.InsertAsync(new ExternalAuthentication { CustomerId = "1" });
        //Act
        await _handler.Handle(new EntityDeleted<Customer>(new Customer { Id = "1" }), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _repository.Table.Count());
    }
}