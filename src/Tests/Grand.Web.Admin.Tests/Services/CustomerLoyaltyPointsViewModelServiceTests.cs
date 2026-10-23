using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Services;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerLoyaltyPointsViewModelServiceTests
{
    private Mock<ILoyaltyPointsService> _loyaltyPointsServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private CustomerLoyaltyPointsViewModelService _customerLoyaltyPointsViewModelService;

    [TestInitialize]
    public void Setup()
    {
        _loyaltyPointsServiceMock = new Mock<ILoyaltyPointsService>();
        _storeServiceMock = new Mock<IStoreService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns<DateTime, DateTimeKind>((dt, _) => dt);

        _customerLoyaltyPointsViewModelService = new CustomerLoyaltyPointsViewModelService(
            _loyaltyPointsServiceMock.Object,
            _storeServiceMock.Object,
            dateTimeServiceMock.Object);
    }

    [TestMethod]
    public async Task PrepareLoyaltyPointsHistoryModel_MapsHistory()
    {
        _loyaltyPointsServiceMock
            .Setup(l => l.GetLoyaltyPointsHistory(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<LoyaltyPointsHistory> {
                new() { StoreId = "store-1", Points = 5, PointsBalance = 10, Message = "added" }
            });
        _storeServiceMock.Setup(s => s.GetStoreById("store-1"))
            .ReturnsAsync(new Grand.Domain.Stores.Store { Id = "store-1", Shortcut = "Store 1" });

        var result = (await _customerLoyaltyPointsViewModelService.PrepareLoyaltyPointsHistoryModel("c1")).ToList();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Store 1", result[0].StoreName);
        Assert.AreEqual(5, result[0].Points);
    }

    [TestMethod]
    public async Task InsertLoyaltyPointsHistory_DelegatesToService()
    {
        var history = new LoyaltyPointsHistory();
        _loyaltyPointsServiceMock
            .Setup(l => l.AddLoyaltyPointsHistory("c1", 10, "store-1", "msg", It.IsAny<string>(), It.IsAny<double>()))
            .ReturnsAsync(history);

        var result = await _customerLoyaltyPointsViewModelService.InsertLoyaltyPointsHistory(
            new Customer { Id = "c1" }, "store-1", 10, "msg");

        Assert.AreSame(history, result);
        _loyaltyPointsServiceMock.Verify(
            l => l.AddLoyaltyPointsHistory("c1", 10, "store-1", "msg", It.IsAny<string>(), It.IsAny<double>()),
            Times.Once);
    }
}
