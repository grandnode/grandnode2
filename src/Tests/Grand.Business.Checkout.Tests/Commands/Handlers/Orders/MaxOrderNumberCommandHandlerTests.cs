using Grand.Business.Checkout.Commands.Handlers.Orders;
using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Commands.Handlers.Orders;

[TestClass]
public class MaxOrderNumberCommandHandlerTests
{
    private MaxOrderNumberCommandHandler _handler;
    private IRepository<Order> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Order>();

        _handler = new MaxOrderNumberCommandHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Act
        var result = await _handler.Handle(new MaxOrderNumberCommand { OrderNumber = 10 }, CancellationToken.None);
        //Assert
        Assert.AreEqual(10, result);
    }
}