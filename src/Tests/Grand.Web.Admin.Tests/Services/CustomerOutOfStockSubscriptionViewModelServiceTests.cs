using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class CustomerOutOfStockSubscriptionViewModelServiceTests
{
    private Mock<IOutOfStockSubscriptionService> _outOfStockSubscriptionServiceMock;
    private Mock<IStoreService> _storeServiceMock;
    private Mock<IProductService> _productServiceMock;
    private Mock<IProductAttributeFormatter> _productAttributeFormatterMock;
    private CustomerOutOfStockSubscriptionViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _outOfStockSubscriptionServiceMock = new Mock<IOutOfStockSubscriptionService>();
        _storeServiceMock = new Mock<IStoreService>();
        _productServiceMock = new Mock<IProductService>();
        _productAttributeFormatterMock = new Mock<IProductAttributeFormatter>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns<DateTime, DateTimeKind>((d, _) => d);

        _service = new CustomerOutOfStockSubscriptionViewModelService(_outOfStockSubscriptionServiceMock.Object,
            _storeServiceMock.Object, _productServiceMock.Object, _productAttributeFormatterMock.Object,
            dateTimeServiceMock.Object, new Mock<IContextAccessor>().Object);
    }

    [TestMethod]
    public async Task PrepareOutOfStockSubscriptionModel_QueriesWithZeroBasedPage()
    {
        _outOfStockSubscriptionServiceMock.Setup(s => s.GetAllSubscriptionsByCustomerId("c1", "", 1, 10))
            .ReturnsAsync(new PagedList<OutOfStockSubscription>(new List<OutOfStockSubscription>(), 1, 10, 0));

        var (models, totalCount) = await _service.PrepareOutOfStockSubscriptionModel("c1", 2, 10);

        Assert.AreEqual(0, models.Count());
        Assert.AreEqual(0, totalCount);
        _outOfStockSubscriptionServiceMock.Verify(s => s.GetAllSubscriptionsByCustomerId("c1", "", 1, 10), Times.Once);
    }

    [TestMethod]
    public async Task PrepareOutOfStockSubscriptionModel_MapsStoreAndProduct_UnknownWhenMissing()
    {
        var subscriptions = new List<OutOfStockSubscription> {
            new() { Id = "o1", StoreId = "s1", ProductId = "p1" },
            new() { Id = "o2", StoreId = "gone", ProductId = "gone" }
        };
        _outOfStockSubscriptionServiceMock.Setup(s => s.GetAllSubscriptionsByCustomerId("c1", "", 0, 10))
            .ReturnsAsync(new PagedList<OutOfStockSubscription>(subscriptions, 0, 10, 2));
        _storeServiceMock.Setup(s => s.GetStoreById("s1")).ReturnsAsync(new Store { Id = "s1", Shortcut = "Main" });
        _productServiceMock.Setup(p => p.GetProductById("p1", false)).ReturnsAsync(new Product { Id = "p1", Name = "Mug" });

        var (models, totalCount) = await _service.PrepareOutOfStockSubscriptionModel("c1", 1, 10);

        var list = models.ToList();
        Assert.AreEqual(2, totalCount);
        Assert.AreEqual("Main", list[0].StoreName);
        Assert.AreEqual("Mug", list[0].ProductName);
        Assert.AreEqual("", list[0].AttributeDescription);
        Assert.AreEqual("Unknown", list[1].StoreName);
        Assert.AreEqual("Unknown", list[1].ProductName);
        _productAttributeFormatterMock.VerifyNoOtherCalls();
    }
}
