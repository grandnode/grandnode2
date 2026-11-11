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
    private readonly Mock<IWarehouseLocationService> _locations = new();
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
            new Mock<ISettingService>().Object, _locations.Object) {
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
    public async Task Store_SaveBatchOfOtherStore_NotWritten()
    {
        _units.Setup(x => x.GetBatch("b1")).ReturnsAsync(new ProductBatch { Id = "b1", StoreId = "store2" });

        var result = await _controller.Batch(new BatchModel { Id = "b1", Supplier = "x" });

        Assert.AreEqual("Batches", (result as RedirectToActionResult)?.ActionName);
        _units.Verify(x => x.UpdateBatchDetails(It.IsAny<ProductBatch>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_SaveSerialNumber_LocationOfOtherWarehouseDropped()
    {
        var serialNumber = new ProductSerialNumber { Id = "sn1", StoreId = StoreId, WarehouseId = "w1" };
        _units.Setup(x => x.GetSerialNumber("sn1")).ReturnsAsync(serialNumber);
        _locations.Setup(x => x.GetLocationById("loc2"))
            .ReturnsAsync(new WarehouseLocation { Id = "loc2", WarehouseId = "w2" });

        await _controller.SerialNumber(new SerialNumberModel {
            Id = "sn1", LocationId = "loc2", Imei = " 490154203237518 ", AdminComment = "  "
        });

        _units.Verify(x => x.UpdateSerialNumberDetails(It.Is<ProductSerialNumber>(s =>
            s.LocationId == null && s.Imei == "490154203237518" && s.AdminComment == null)), Times.Once);
    }

    [TestMethod]
    public async Task SaveSerialNumber_DateNotUnderstood_NotWritten()
    {
        _units.Setup(x => x.GetSerialNumber("sn1"))
            .ReturnsAsync(new ProductSerialNumber { Id = "sn1", StoreId = StoreId });
        _controller.ModelState.AddModelError(nameof(SerialNumberModel.ManufacturedOn), "not a date");

        var result = await _controller.SerialNumber(new SerialNumberModel { Id = "sn1" });

        Assert.AreEqual("SerialNumber", (result as RedirectToActionResult)?.ActionName);
        _units.Verify(x => x.UpdateSerialNumberDetails(It.IsAny<ProductSerialNumber>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveBatch_DateNotUnderstood_NotWritten()
    {
        _units.Setup(x => x.GetBatch("b1")).ReturnsAsync(new ProductBatch { Id = "b1", StoreId = StoreId });
        _controller.ModelState.AddModelError(nameof(BatchModel.ManufacturedOn), "not a date");

        var result = await _controller.Batch(new BatchModel { Id = "b1" });

        Assert.AreEqual("Batch", (result as RedirectToActionResult)?.ActionName);
        _units.Verify(x => x.UpdateBatchDetails(It.IsAny<ProductBatch>()), Times.Never);
    }

    [TestMethod]
    public async Task SerialNumber_DateReadBackInLocalTime_ShownAsStored()
    {
        //LiteDB reads the midnight UTC stored back in local time
        var stored = new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);
        _units.Setup(x => x.GetSerialNumber("sn1")).ReturnsAsync(new ProductSerialNumber {
            Id = "sn1", StoreId = StoreId, ManufacturedOnUtc = stored.ToLocalTime(),
            SupplierWarrantyUntilUtc = stored.AddYears(2).ToLocalTime()
        });

        var model = (await _controller.SerialNumber("sn1") as ViewResult)?.Model as SerialNumberModel;

        Assert.AreEqual(stored, model?.ManufacturedOn);
        Assert.AreEqual(DateTimeKind.Utc, model?.ManufacturedOn?.Kind);
        Assert.AreEqual(stored.AddYears(2), model?.SupplierWarrantyUntil);
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
