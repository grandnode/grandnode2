using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class StockHistoryControllerTests
{
    private const string StoreId = "store1";

    private Mock<IInventoryJournalService> _journalService;
    private Mock<IProductService> _productService;
    private Mock<IAdminDataScope<Product>> _productScope;
    private Mock<IWarehouseService> _warehouseService;
    private StoreWarehouseDataScope _warehouseScope;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "staff", StaffStoreId = StoreId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);
        _warehouseScope = new StoreWarehouseDataScope(contextAccessor.Object);

        _journalService = new Mock<IInventoryJournalService>();
        _journalService.Setup(x => x.GetProductHistory(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<IList<string>>(), It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync(new PagedList<InventoryJournal>());
        _productService = new Mock<IProductService>();
        _productScope = new Mock<IAdminDataScope<Product>>();
        _productScope.Setup(x => x.DefaultStoreId).Returns(StoreId);
        _productScope.Setup(x => x.GetDefaultStoreIdAsync()).ReturnsAsync(StoreId);
        _productScope.Setup(x => x.ResourceKeyPrefix).Returns("Admin");
        _warehouseService = new Mock<IWarehouseService>();
        _warehouseService.Setup(x => x.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>());
        _warehouseService.Setup(x => x.GetWarehouseById("w-other"))
            .ReturnsAsync(new Warehouse { Id = "w-other", StoreId = "store2" });
    }

    private static T WithContext<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private ProductStockHistoryController History() => WithContext(new ProductStockHistoryController(
        _productService.Object, _journalService.Object, _warehouseService.Object, new Mock<ICustomerService>().Object,
        new Mock<IEnumTranslationService>().Object, new Mock<IDateTimeService>().Object,
        new Mock<IRequestTranslationService>().Object, _productScope.Object));

    [TestMethod]
    public async Task Store_StockHistory_PassesStaffAndSharedStoreIds()
    {
        var product = new Product();
        _productService.Setup(x => x.GetProductById(product.Id, false)).ReturnsAsync(product);
        _productScope.Setup(x => x.CanView(product)).ReturnsAsync(true);

        await History().ListStockHistory(new DataSourceRequest { Page = 1, PageSize = 10 }, product.Id, "", null,
            null);

        _journalService.Verify(x => x.GetProductHistory(product.Id, "", null, null,
            It.Is<IList<string>>(s => s.Count == 2 && s.Contains(StoreId) && s.Contains("")), 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task Store_StockHistory_ProductNotViewable_Error()
    {
        var product = new Product();
        _productService.Setup(x => x.GetProductById(product.Id, false)).ReturnsAsync(product);
        _productScope.Setup(x => x.CanView(product)).ReturnsAsync(false);

        await History().ListStockHistory(new DataSourceRequest { Page = 1, PageSize = 10 }, product.Id, "", null,
            null);

        _journalService.Verify(x => x.GetProductHistory(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<IList<string>>(), It.IsAny<int>(),
            It.IsAny<int>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_StockAtDate_OtherStoreWarehouse_AccessDenied()
    {
        var controller = WithContext(new InventoryReportController(_journalService.Object,
            new Mock<IInventoryDocumentViewModelService>().Object, _warehouseService.Object, _productService.Object,
            new Mock<IDateTimeService>().Object, new Mock<IRequestTranslationService>().Object, _warehouseScope));

        await controller.StockAtDateData(new DataSourceRequest(), "w-other", null);

        _journalService.Verify(x => x.GetStockAtDate(It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }
}
