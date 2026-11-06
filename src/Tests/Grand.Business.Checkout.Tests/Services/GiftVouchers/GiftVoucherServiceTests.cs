using Grand.Business.Checkout.Services.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.GiftVouchers;

[TestClass]
public class GiftVoucherServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<GiftVoucher>> _repositoryMock;
    private IGiftVoucherService _service;

    [TestInitialize]
    public void Init()
    {
        _repositoryMock = new Mock<IRepository<GiftVoucher>>();
        _mediatorMock = new Mock<IMediator>();
        _service = new GiftVoucherService(_repositoryMock.Object, _mediatorMock.Object, new MongoResourceLockTest());
    }

    private static (IRepository<GiftVoucher> repository, IGiftVoucherService service) StoredVouchers()
    {
        var repository = new MongoDBRepositoryTest<GiftVoucher>();
        return (repository, new GiftVoucherService(repository, new Mock<IMediator>().Object,
            new MongoResourceLockTest()));
    }

    private static async Task<GiftVoucher> InsertVoucher(IRepository<GiftVoucher> repository, double amount,
        bool activated = true, params double[] used)
    {
        var giftVoucher = new GiftVoucher { Code = "code", Amount = amount, IsGiftVoucherActivated = activated };
        foreach (var value in used)
            giftVoucher.GiftVoucherUsageHistory.Add(new GiftVoucherUsageHistory {
                GiftVoucherId = giftVoucher.Id, UsedWithOrderId = $"used-{value}", UsedValue = value
            });
        await repository.InsertAsync(giftVoucher);
        return giftVoucher;
    }

    [TestMethod]
    public async Task TryRedeem_BalanceCovers_AddsOneUsageEntry()
    {
        var (repository, service) = StoredVouchers();
        var giftVoucher = await InsertVoucher(repository, 100);

        var redeemed = await service.TryRedeem(giftVoucher.Id, 40, "o1");

        Assert.IsTrue(redeemed);
        var history = (await repository.GetByIdAsync(giftVoucher.Id)).GiftVoucherUsageHistory;
        Assert.HasCount(1, history);
        Assert.AreEqual(40, history.First().UsedValue);
        Assert.AreEqual("o1", history.First().UsedWithOrderId);
        Assert.AreEqual(giftVoucher.Id, history.First().GiftVoucherId);
    }

    [TestMethod]
    public async Task TryRedeem_BalanceShort_ReturnsFalseAndWritesNothing()
    {
        var (repository, service) = StoredVouchers();
        var giftVoucher = await InsertVoucher(repository, 100, true, 70);

        var redeemed = await service.TryRedeem(giftVoucher.Id, 40, "o1");

        Assert.IsFalse(redeemed);
        Assert.HasCount(1, (await repository.GetByIdAsync(giftVoucher.Id)).GiftVoucherUsageHistory);
    }

    [TestMethod]
    public async Task TryRedeem_NotActivated_ReturnsFalse()
    {
        var (repository, service) = StoredVouchers();
        var giftVoucher = await InsertVoucher(repository, 100, false);

        Assert.IsFalse(await service.TryRedeem(giftVoucher.Id, 40, "o1"));
        Assert.IsEmpty((await repository.GetByIdAsync(giftVoucher.Id)).GiftVoucherUsageHistory);
    }

    [TestMethod]
    public async Task TryRedeem_RemainingEqualsAmountWithRoundingNoise_Succeeds()
    {
        var (repository, service) = StoredVouchers();
        var giftVoucher = await InsertVoucher(repository, 0.3, true, 0.1);

        Assert.IsTrue(await service.TryRedeem(giftVoucher.Id, 0.2, "o1"));
    }

    [TestMethod]
    public async Task TryRedeem_ParallelRedemptionsOfFullBalance_OnlyOneSucceeds()
    {
        var (repository, service) = StoredVouchers();
        var giftVoucher = await InsertVoucher(repository, 100);

        var results = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(i => Task.Run(() => service.TryRedeem(giftVoucher.Id, 100, $"o{i}"))));

        Assert.AreEqual(1, results.Count(x => x));
        Assert.HasCount(1, (await repository.GetByIdAsync(giftVoucher.Id)).GiftVoucherUsageHistory);
    }

    [TestMethod]
    public async Task CancelRedemption_RemovesOnlyThatOrdersEntry()
    {
        var (repository, service) = StoredVouchers();
        var giftVoucher = await InsertVoucher(repository, 100);
        await service.TryRedeem(giftVoucher.Id, 10, "o1");
        await service.TryRedeem(giftVoucher.Id, 20, "o2");

        await service.CancelRedemption(giftVoucher.Id, "o1");

        var history = (await repository.GetByIdAsync(giftVoucher.Id)).GiftVoucherUsageHistory;
        Assert.HasCount(1, history);
        Assert.AreEqual("o2", history.First().UsedWithOrderId);
    }

    [TestMethod]
    public void GenerateGiftVoucherCode_ShouldReturnUniqueValues()
    {
        var results = new List<string>();
        results.Add(_service.GenerateGiftVoucherCode());
        results.Add(_service.GenerateGiftVoucherCode());
        results.Add(_service.GenerateGiftVoucherCode());
        results.Add(_service.GenerateGiftVoucherCode());
        results.Add(_service.GenerateGiftVoucherCode());
        results.Add(_service.GenerateGiftVoucherCode());
        results.Add(_service.GenerateGiftVoucherCode());

        Assert.IsTrue(results.Count.Equals(results.Distinct().Count()));
    }

    [TestMethod]
    public async Task InsertGiftVoucher_InovokeExpectedMethods()
    {
        await _service.InsertGiftVoucher(new GiftVoucher { Code = "code" });
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<GiftVoucher>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<GiftVoucher>>(), default), Times.Once);
    }

    [TestMethod]
    public void InsertGiftVoucher_NullArgument_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.InsertGiftVoucher(null));
    }

    [TestMethod]
    public async Task UpdateGiftVoucher_InovokeExpectedMethods()
    {
        await _service.UpdateGiftVoucher(new GiftVoucher { Code = "code" });
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<GiftVoucher>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<GiftVoucher>>(), default), Times.Once);
    }

    [TestMethod]
    public void UpdateGiftVoucher_NullArgument_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.UpdateGiftVoucher(null));
    }

    [TestMethod]
    public async Task DeleteGiftVoucher_InovokeExpectedMethods()
    {
        await _service.DeleteGiftVoucher(new GiftVoucher());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<GiftVoucher>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<GiftVoucher>>(), default), Times.Once);
    }

    [TestMethod]
    public void DeleteGiftVoucher_NullArgument_ThrowException()
    {
        Assert.ThrowsExactlyAsync<ArgumentNullException>(async () => await _service.DeleteGiftVoucher(null));
    }
}