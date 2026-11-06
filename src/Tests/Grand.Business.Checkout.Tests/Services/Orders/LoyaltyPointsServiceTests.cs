using Grand.Business.Checkout.Services.Orders;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using Grand.SharedKernel.Extensions;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Checkout.Tests.Services.Orders;

[TestClass]
public class LoyaltyPointsServiceTests
{
    private LoyaltyPointsService _loyaltyPointsService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<LoyaltyPointsHistory> _repository;

    [TestInitialize]
    public void Init()
    {

        var settingsPath = Path.Combine("", CommonPath.AppData, CommonPath.SettingsFile);
        DataSettingsManager.Initialize(settingsPath);

        _repository = new MongoDBRepositoryTest<LoyaltyPointsHistory>();
        _mediatorMock = new Mock<IMediator>();
        _loyaltyPointsService = new LoyaltyPointsService(_repository, new LoyaltyPointsSettings(),
            _mediatorMock.Object, new MongoResourceLockTest());
    }

    [TestMethod]
    public async Task TryRedeem_BalanceShort_ReturnsNullAndWritesNothing()
    {
        await _loyaltyPointsService.AddLoyaltyPointsHistory("c1", 500, "s1");

        var redeemed = await _loyaltyPointsService.TryRedeemLoyaltyPoints("c1", 1000, "s1", "redeemed", "o1", 10);

        Assert.IsNull(redeemed);
        Assert.AreEqual(500, await _loyaltyPointsService.GetLoyaltyPointsBalance("c1", "s1"));
        Assert.HasCount(1, _repository.Table.Where(x => x.CustomerId == "c1").ToList());
    }

    [TestMethod]
    public async Task TryRedeem_BalanceCovers_WritesTheRedemption()
    {
        await _loyaltyPointsService.AddLoyaltyPointsHistory("c1", 500, "s1");

        var redeemed = await _loyaltyPointsService.TryRedeemLoyaltyPoints("c1", 200, "s1", "redeemed", "o1", 10);

        Assert.IsNotNull(redeemed);
        Assert.AreEqual(-200, redeemed.Points);
        Assert.AreEqual(300, redeemed.PointsBalance);
        Assert.AreEqual("o1", redeemed.UsedWithOrderId);
        Assert.AreEqual(10, redeemed.UsedAmount);
        Assert.AreEqual(300, await _loyaltyPointsService.GetLoyaltyPointsBalance("c1", "s1"));
    }

    [TestMethod]
    public async Task TryRedeem_ParallelRedemptionsOfWholeBalance_OnlyOneSucceeds()
    {
        await _loyaltyPointsService.AddLoyaltyPointsHistory("c1", 1000, "s1");

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(() =>
            _loyaltyPointsService.TryRedeemLoyaltyPoints("c1", 1000, "s1", "redeemed", $"o{i}", 10))));

        Assert.AreEqual(1, results.Count(x => x != null));
        Assert.AreEqual(0, await _loyaltyPointsService.GetLoyaltyPointsBalance("c1", "s1"));
    }

    [TestMethod]
    public async Task AddLoyaltyPointsHistory_ParallelAdds_BalanceCountsEveryAdd()
    {
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            _loyaltyPointsService.AddLoyaltyPointsHistory("c1", 10, "s1"))));

        Assert.AreEqual(100, await _loyaltyPointsService.GetLoyaltyPointsBalance("c1", "s1"));
    }

    [TestMethod]
    public async Task TryRedeem_PointsSharedAcrossStores_SeesOtherStoresPoints()
    {
        var service = new LoyaltyPointsService(_repository,
            new LoyaltyPointsSettings { PointsAccumulatedForAllStores = true }, _mediatorMock.Object,
            new MongoResourceLockTest());
        await service.AddLoyaltyPointsHistory("c1", 1000, "storeA");

        var redeemed = await service.TryRedeemLoyaltyPoints("c1", 1000, "storeB", "redeemed", "o1", 10);

        Assert.IsNotNull(redeemed);
        Assert.AreEqual(0, await service.GetLoyaltyPointsBalance("c1", "storeA"));
    }

    [TestMethod]
    public async Task GetLoyaltyPointsBalanceTest()
    {
        //Assert
        await _repository.InsertAsync(new LoyaltyPointsHistory
            { CustomerId = "1", StoreId = "", Points = 10, PointsBalance = 10 });
        await _repository.InsertAsync(new LoyaltyPointsHistory
            { CustomerId = "1", StoreId = "", Points = 10, PointsBalance = 20 });
        await _repository.InsertAsync(new LoyaltyPointsHistory
            { CustomerId = "1", StoreId = "", Points = 10, PointsBalance = 30 });
        //Act
        var result = await _loyaltyPointsService.GetLoyaltyPointsBalance("1", "");
        //Assert
        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public async Task AddLoyaltyPointsHistoryTest()
    {
        //Act
        var result = await _loyaltyPointsService.AddLoyaltyPointsHistory("1", 10, "");
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(10, result.Points);
        Assert.IsNotEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task GetLoyaltyPointsHistoryTest()
    {
        //Assert
        await _repository.InsertAsync(new LoyaltyPointsHistory
            { CustomerId = "1", StoreId = "", Points = 10, PointsBalance = 10 });
        await _repository.InsertAsync(new LoyaltyPointsHistory
            { CustomerId = "1", StoreId = "", Points = 10, PointsBalance = 20 });
        await _repository.InsertAsync(new LoyaltyPointsHistory
            { CustomerId = "1", StoreId = "", Points = 10, PointsBalance = 30 });
        //Act
        var result = await _loyaltyPointsService.GetLoyaltyPointsHistory("1");
        //Assert
        Assert.HasCount(3, result);
    }
}