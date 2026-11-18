using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Payments;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetPaymentTransactionQueryHandlerTests
{
    private GetPaymentTransactionQueryHandler _handler;
    private MongoDBRepositoryTest<PaymentTransaction> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<PaymentTransaction>();
        _handler = new GetPaymentTransactionQueryHandler(_repository);
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
}
