using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class ProductUnitControllerTests
{
    private const string StoreId = "store1";
    private readonly Mock<IProductUnitService> _units = new();
    private readonly Mock<IWarehouseService> _warehouses = new();
    private ProductUnitController _controller;

    [TestInitialize]
    public void Init()
    {
        var productScope = new Mock<IAdminDataScope<Product>>();
        productScope.Setup(x => x.GetDefaultStoreIdAsync()).ReturnsAsync(StoreId);
        _controller = new ProductUnitController(_units.Object, new Mock<IProductService>().Object,
            _warehouses.Object, new Mock<IDateTimeService>().Object,
            new Mock<IEnumTranslationService>().Object, new Mock<IRequestTranslationService>().Object,
            new Mock<IInventoryDocumentViewModelService>().Object, productScope.Object,
            new Mock<ISettingService>().Object) {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [TestMethod]
    public async Task Store_SerialNumberOfOtherStore_AccessDenied()
    {
        _units.Setup(x => x.GetSerialNumber("sn1"))
            .ReturnsAsync(new ProductSerialNumber { Id = "sn1", StoreId = "store2" });

        var result = await _controller.SerialNumber("sn1");

        Assert.AreEqual("SerialNumbers", (result as RedirectToActionResult)?.ActionName);
    }

    [TestMethod]
    public async Task Store_SerialNumberHistory_OnlyOwnWarehouses()
    {
        var serialNumber = new ProductSerialNumber { Id = "sn1", ProductId = "p1", Number = "A1", StoreId = StoreId };
        _units.Setup(x => x.GetSerialNumber("sn1")).ReturnsAsync(serialNumber);
        _units.Setup(x => x.History(serialNumber, It.IsAny<IList<string>>(), 0, 10))
            .ReturnsAsync(new PagedList<InventoryJournal>(new List<InventoryJournal>(), 0, 10));
        _warehouses.Setup(x => x.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>(new List<Warehouse> {
                new() { Id = "w1", StoreId = StoreId }, new() { Id = "w2", StoreId = "store2" }, new() { Id = "w3" }
            }, 0, 10));

        await _controller.SerialNumberHistoryList(new DataSourceRequest { Page = 1, PageSize = 10 }, "sn1");

        _units.Verify(x => x.History(serialNumber, It.Is<IList<string>>(w => w.SequenceEqual(new[] { "w1" })), 0, 10),
            Times.Once);
    }

    [TestMethod]
    public async Task Store_BatchHistoryOfOtherStore_NotRead()
    {
        _units.Setup(x => x.GetBatch("b1")).ReturnsAsync(new ProductBatch { Id = "b1", StoreId = "store2" });

        await _controller.BatchHistoryList(new DataSourceRequest { Page = 1, PageSize = 10 }, "b1");
        await _controller.BatchRecipientList(new DataSourceRequest { Page = 1, PageSize = 10 }, "b1");

        _units.Verify(x => x.History(It.IsAny<ProductBatch>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _units.Verify(x => x.BatchRecipients(It.IsAny<ProductBatch>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_BatchRecipientsOfOtherStore_AccessDenied()
    {
        _units.Setup(x => x.GetBatch("b1")).ReturnsAsync(new ProductBatch { Id = "b1", StoreId = "" });

        var result = await _controller.BatchRecipientsCsv("b1");

        Assert.AreEqual("Batches", (result as RedirectToActionResult)?.ActionName);
        _units.Verify(x => x.BatchRecipients(It.IsAny<ProductBatch>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_Batches_OnlyOwnWarehouses()
    {
        _units.Setup(x => x.SearchBatches(It.IsAny<UnitSearch>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<ProductBatch>(new List<ProductBatch>(), 0, 10));

        await _controller.BatchList(new DataSourceRequest { Page = 1, PageSize = 10 }, new BatchListModel());

        _units.Verify(x => x.SearchBatches(It.Is<UnitSearch>(s => s.StoreId == StoreId), 0, 10), Times.Once);
    }
}
