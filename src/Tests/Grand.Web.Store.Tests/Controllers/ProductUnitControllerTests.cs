using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Catalog;
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
    private ProductUnitController _controller;

    [TestInitialize]
    public void Init()
    {
        var productScope = new Mock<IAdminDataScope<Product>>();
        productScope.Setup(x => x.GetDefaultStoreIdAsync()).ReturnsAsync(StoreId);
        _controller = new ProductUnitController(_units.Object, new Mock<IProductService>().Object,
            new Mock<IWarehouseService>().Object, new Mock<IDateTimeService>().Object,
            new Mock<IEnumTranslationService>().Object, new Mock<IRequestTranslationService>().Object,
            new Mock<IInventoryDocumentViewModelService>().Object, productScope.Object) {
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
    public async Task Store_BatchRecipientsOfOtherStore_AccessDenied()
    {
        _units.Setup(x => x.GetBatch("b1")).ReturnsAsync(new ProductBatch { Id = "b1", StoreId = "" });

        var result = await _controller.BatchRecipientsCsv("b1");

        Assert.AreEqual("Batches", (result as RedirectToActionResult)?.ActionName);
        _units.Verify(x => x.BatchRecipients(It.IsAny<ProductBatch>()), Times.Never);
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
