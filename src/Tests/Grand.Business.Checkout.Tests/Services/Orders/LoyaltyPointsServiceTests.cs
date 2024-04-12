using Grand.Business.Checkout.Services.Orders;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Orders;
using MediatR;
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
        _repository = new MongoDBRepositoryTest<LoyaltyPointsHistory>();
        _mediatorMock = new Mock<IMediator>();

        _loyaltyPointsService =
            new LoyaltyPointsService(_repository, new LoyaltyPointsSettings(), _mediatorMock.Object);
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
        Assert.IsTrue(_repository.Table.Any());
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
        Assert.AreEqual(3, result.Count);
    }
}