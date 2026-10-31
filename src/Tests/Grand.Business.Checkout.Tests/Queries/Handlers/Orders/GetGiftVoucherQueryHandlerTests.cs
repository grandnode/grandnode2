using Grand.Business.Checkout.Queries.Handlers.Orders;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Data.Tests.LiteDb;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Checkout.Tests.Queries.Handlers.Orders;

[TestClass]
public class GetGiftVoucherQueryHandlerTests
{
    private GetGiftVoucherQueryHandler _handler;
    private LiteDBRepositoryMock<GiftVoucher> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new LiteDBRepositoryMock<GiftVoucher>();
        _handler = new GetGiftVoucherQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Handle_ByPurchasedOrderItem_SkipsVouchersCreatedByHand()
    {
        await _repository.InsertAsync(new GiftVoucher { Code = "manual" });
        await _repository.InsertAsync(new GiftVoucher { Code = "bought", PurchasedWithOrderItem = new OrderItem { Id = "item-1" } });
        await _repository.InsertAsync(new GiftVoucher { Code = "other", PurchasedWithOrderItem = new OrderItem { Id = "item-2" } });

        var result = await _handler.Handle(new GetGiftVoucherQuery { PurchasedWithOrderItemId = "item-1" }, CancellationToken.None);

        Assert.HasCount(1, result);
        Assert.AreEqual("bought", result[0].Code);
    }
}
