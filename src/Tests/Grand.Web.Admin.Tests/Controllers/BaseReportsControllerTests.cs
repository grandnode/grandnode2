#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Business.Core.Utilities.System;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseReportsControllerTests
{
    private class TestReportsController(
        IOrderReportService orderReportService,
        IProductsReportService productsReportService,
        ICustomerReportViewModelService customerReportViewModelService,
        IPriceFormatter priceFormatter,
        ICurrencyService currencyService,
        IProductService productService,
        IProductAttributeFormatter productAttributeFormatter,
        IStockQuantityService stockQuantityService,
        ITranslationService translationService,
        IStoreService storeService,
        ICountryService countryService,
        IVendorService vendorService,
        IDateTimeService dateTimeService,
        IOrderStatusService orderStatusService,
        IEnumTranslationService enumTranslationService,
        IContextAccessor contextAccessor,
        IReportDataScope scope)
        : BaseReportsController(orderReportService, productsReportService, customerReportViewModelService,
            priceFormatter, currencyService, productService, productAttributeFormatter, stockQuantityService,
            translationService, storeService, countryService, vendorService, dateTimeService,
            orderStatusService, enumTranslationService, contextAccessor, scope);

    private TestReportsController _controller = null!;
    private Mock<IOrderReportService> _orderReportServiceMock = null!;
    private Mock<IProductsReportService> _productsReportServiceMock = null!;
    private Mock<ICustomerReportViewModelService> _customerReportViewModelServiceMock = null!;
    private Mock<IProductService> _productServiceMock = null!;
    private Mock<IStoreService> _storeServiceMock = null!;
    private Mock<IVendorService> _vendorServiceMock = null!;
    private Mock<IReportDataScope> _scopeMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _orderReportServiceMock = new Mock<IOrderReportService>();
        _productsReportServiceMock = new Mock<IProductsReportService>();
        _customerReportViewModelServiceMock = new Mock<ICustomerReportViewModelService>();
        _customerReportViewModelServiceMock.Setup(s => s.PrepareCustomerReportsModel()).ReturnsAsync(new CustomerReportsModel());
        var priceFormatterMock = new Mock<IPriceFormatter>();
        priceFormatterMock.Setup(p => p.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>())).Returns("$0.00");
        var currencyServiceMock = new Mock<ICurrencyService>();
        currencyServiceMock.Setup(c => c.GetPrimaryStoreCurrency()).ReturnsAsync(new Currency());
        _productServiceMock = new Mock<IProductService>();
        var productAttributeFormatterMock = new Mock<IProductAttributeFormatter>();
        var stockQuantityServiceMock = new Mock<IStockQuantityService>();
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        _storeServiceMock = new Mock<IStoreService>();
        _storeServiceMock.Setup(s => s.GetAllStores()).ReturnsAsync(new List<Store>());
        var countryServiceMock = new Mock<ICountryService>();
        countryServiceMock.Setup(c => c.GetAllCountriesForBilling("", "", true)).ReturnsAsync(new List<Country>());
        _vendorServiceMock = new Mock<IVendorService>();
        _vendorServiceMock.Setup(v => v.GetAllVendors("", 0, int.MaxValue, true))
            .ReturnsAsync(new PagedList<Vendor>(new List<Vendor>(), 0, int.MaxValue));
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        var orderStatusServiceMock = new Mock<IOrderStatusService>();
        orderStatusServiceMock.Setup(o => o.GetAll()).ReturnsAsync(new List<Grand.Domain.Orders.OrderStatus>());
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        enumTranslationServiceMock.Setup(e => e.ToSelectList(Grand.Domain.Payments.PaymentStatus.Pending, false, null))
            .Returns(new Microsoft.AspNetCore.Mvc.Rendering.SelectList(new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()));
        var contextAccessorMock = new Mock<IContextAccessor>();
        _scopeMock = new Mock<IReportDataScope>();
        _scopeMock.Setup(s => s.StoreId).Returns("");
        _scopeMock.Setup(s => s.VendorId).Returns("");
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(true);
        _scopeMock.Setup(s => s.ShowVendorSelector).Returns(true);
        _scopeMock.Setup(s => s.ResourceKeyPrefix).Returns("Admin");
        _scopeMock.Setup(s => s.CanIncludeProduct(It.IsAny<Product>())).Returns(true);

        _controller = new TestReportsController(_orderReportServiceMock.Object, _productsReportServiceMock.Object,
            _customerReportViewModelServiceMock.Object, priceFormatterMock.Object, currencyServiceMock.Object,
            _productServiceMock.Object, productAttributeFormatterMock.Object, stockQuantityServiceMock.Object,
            translationServiceMock.Object, _storeServiceMock.Object, countryServiceMock.Object,
            _vendorServiceMock.Object, dateTimeServiceMock.Object, orderStatusServiceMock.Object,
            enumTranslationServiceMock.Object, contextAccessorMock.Object, _scopeMock.Object);
    }

    [TestMethod]
    public async Task BestsellersBriefReportByQuantityList_NoPermissionCheck_AlwaysReturnsJson()
    {
        _orderReportServiceMock.Setup(o => o.BestSellersReport("", "", null, null, null, null, null, "", 1, 0,
                10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine>(), 0, 10));

        var result = await _controller.BestsellersBriefReportByQuantityList(new DataSourceRequest { Page = 1, PageSize = 10 });

        Assert.IsInstanceOfType(result, typeof(JsonResult));
    }

    [TestMethod]
    public async Task BestsellersBriefReportByQuantityList_ScopeValuesThreadedIntoQuery()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _orderReportServiceMock.Setup(o => o.BestSellersReport("store-1", "vendor-1", null, null, null, null, null,
                "", 1, 0, 10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine>(), 0, 0));

        await _controller.BestsellersBriefReportByQuantityList(new DataSourceRequest { Page = 1, PageSize = 10 });

        _orderReportServiceMock.Verify(o => o.BestSellersReport("store-1", "vendor-1", null, null, null, null, null,
            "", 1, 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task BestsellersBriefReportByAmountList_ScopeValuesThreadedIntoQuery()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _orderReportServiceMock.Setup(o => o.BestSellersReport("store-1", "vendor-1", null, null, null, null, null,
                "", 2, 0, 10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine>(), 0, 0));

        await _controller.BestsellersBriefReportByAmountList(new DataSourceRequest { Page = 1, PageSize = 10 });

        _orderReportServiceMock.Verify(o => o.BestSellersReport("store-1", "vendor-1", null, null, null, null, null,
            "", 2, 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task BestsellersBriefReportByQuantityList_CanIncludeProductFalse_DropsRow()
    {
        var line = new BestsellersReportLine { ProductId = "p1", TotalAmount = 1, TotalQuantity = 1 };
        _orderReportServiceMock.Setup(o => o.BestSellersReport("", "", null, null, null, null, null, "", 1, 0, 10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine> { line }, 0, 1));
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(new Product { Id = "p1" });
        _scopeMock.Setup(s => s.CanIncludeProduct(It.IsAny<Product>())).Returns(false);

        var result = await _controller.BestsellersBriefReportByQuantityList(new DataSourceRequest { Page = 1, PageSize = 10 }) as JsonResult;

        var gridModel = (DataSourceResult)result!.Value!;
        Assert.AreEqual(0, ((List<BestsellersReportLineModel>)gridModel.Data).Count);
    }

    [TestMethod]
    public async Task BestsellersReport_ShowStoreSelectorTrue_PopulatesAvailableStores()
    {
        var result = await _controller.BestsellersReport() as ViewResult;

        Assert.IsNotNull(result);
        var model = (BestsellersReportModel)result!.Model!;
        Assert.IsTrue(model.AvailableStores.Count > 0);
        Assert.IsTrue(model.AvailableVendors.Count > 0);
    }

    [TestMethod]
    public async Task BestsellersReport_ShowStoreSelectorFalse_SkipsAvailableStoresAndVendors()
    {
        _scopeMock.Setup(s => s.ShowStoreSelector).Returns(false);
        _scopeMock.Setup(s => s.ShowVendorSelector).Returns(false);

        var result = await _controller.BestsellersReport() as ViewResult;

        var model = (BestsellersReportModel)result!.Model!;
        Assert.AreEqual(0, model.AvailableStores.Count);
        Assert.AreEqual(0, model.AvailableVendors.Count);
        _storeServiceMock.Verify(s => s.GetAllStores(), Times.Never);
        _vendorServiceMock.Verify(v => v.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task BestsellersReportList_ScopeStoreIdNonEmpty_OverwritesPostedStoreId()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _orderReportServiceMock.Setup(o => o.BestSellersReport("store-1", "", null, null, null, null, null, "", 2,
                0, 10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine>(), 0, 0));

        var model = new BestsellersReportModel { StoreId = "posted-store-should-be-overwritten" };
        await _controller.BestsellersReportList(new DataSourceRequest { Page = 1, PageSize = 10 }, model);

        _orderReportServiceMock.Verify(o => o.BestSellersReport("store-1", "", null, null, null, null, null, "", 2,
            0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task BestsellersReportList_CanIncludeProductFalse_DropsRow()
    {
        var line = new BestsellersReportLine { ProductId = "p1", TotalAmount = 1, TotalQuantity = 1 };
        _orderReportServiceMock.Setup(o => o.BestSellersReport("", "", null, null, null, null, null, "", 2, 0, 10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine> { line }, 0, 1));
        _productServiceMock.Setup(p => p.GetProductById("p1")).ReturnsAsync(new Product { Id = "p1" });
        _scopeMock.Setup(s => s.CanIncludeProduct(It.IsAny<Product>())).Returns(false);

        var result = await _controller.BestsellersReportList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new BestsellersReportModel()) as JsonResult;

        var gridModel = (DataSourceResult)result!.Value!;
        Assert.AreEqual(0, ((List<BestsellersReportLineModel>)gridModel.Data).Count);
    }

    [TestMethod]
    public void NeverSoldReport_ReturnsViewWithModel()
    {
        var result = _controller.NeverSoldReport() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(NeverSoldReportModel));
    }

    [TestMethod]
    public async Task NeverSoldReportList_ScopeValuesThreadedIntoQuery()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _orderReportServiceMock.Setup(o => o.ProductsNeverSold("store-1", "vendor-1", null, null, 0, 10, true))
            .ReturnsAsync(new PagedList<Product>(new List<Product>(), 0, 0));

        await _controller.NeverSoldReportList(new DataSourceRequest { Page = 1, PageSize = 10 }, new NeverSoldReportModel());

        _orderReportServiceMock.Verify(o => o.ProductsNeverSold("store-1", "vendor-1", null, null, 0, 10, true), Times.Once);
    }

    [TestMethod]
    public async Task CountryReport_NoPermissionCheck_ReturnsViewWithModel()
    {
        var result = await _controller.CountryReport() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(CountryReportModel));
    }

    [TestMethod]
    public async Task CountryReportList_ScopeValuesThreadedIntoQuery()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _orderReportServiceMock.Setup(o => o.GetCountryReport("store-1", "vendor-1", null, null, null, null, null))
            .ReturnsAsync(new List<OrderByCountryReportLine>());

        await _controller.CountryReportList(new DataSourceRequest { Page = 1, PageSize = 10 }, new CountryReportModel());

        _orderReportServiceMock.Verify(o => o.GetCountryReport("store-1", "vendor-1", null, null, null, null, null), Times.Once);
    }

    [TestMethod]
    public void LowStockReport_ReturnsView()
    {
        var result = _controller.LowStockReport();
        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task LowStockReportList_ScopeValuesThreadedIntoQuery()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _productsReportServiceMock.Setup(p => p.LowStockProducts("vendor-1", "store-1"))
            .ReturnsAsync((new List<Product>(), new List<ProductsAttributeCombination>()));

        await _controller.LowStockReportList(new DataSourceRequest { Page = 1, PageSize = 10 });

        _productsReportServiceMock.Verify(p => p.LowStockProducts("vendor-1", "store-1"), Times.Once);
    }

    [TestMethod]
    public async Task Customer_NoPermissionCheck_ReturnsViewWithModel()
    {
        var result = await _controller.Customer() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(CustomerReportsModel));
    }

    [TestMethod]
    public async Task ReportBestCustomersByOrderTotalList_PassesScopeVendorIdToService()
    {
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _customerReportViewModelServiceMock.Setup(s => s.PrepareBestCustomerReportLineModel(It.IsAny<BestCustomersReportModel>(), 1, 1, 10, "vendor-1"))
            .ReturnsAsync((new List<BestCustomerReportLineModel>(), 0));

        await _controller.ReportBestCustomersByOrderTotalList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new BestCustomersReportModel());

        _customerReportViewModelServiceMock.Verify(s => s.PrepareBestCustomerReportLineModel(It.IsAny<BestCustomersReportModel>(), 1, 1, 10, "vendor-1"),
            Times.Once);
    }
}
