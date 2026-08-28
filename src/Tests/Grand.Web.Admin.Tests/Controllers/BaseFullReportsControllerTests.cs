#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Business.Core.Utilities.System;
using Grand.Domain;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseFullReportsControllerTests
{
    private class TestFullReportsController(
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
        IReportDataScope scope,
        IOrderService orderService,
        ICustomerReportService customerReportService,
        IPermissionService permissionService)
        : BaseFullReportsController(orderReportService, productsReportService, customerReportViewModelService,
            priceFormatter, currencyService, productService, productAttributeFormatter, stockQuantityService,
            translationService, storeService, countryService, vendorService, dateTimeService,
            orderStatusService, enumTranslationService, contextAccessor, scope, orderService,
            customerReportService, permissionService);

    private TestFullReportsController _controller = null!;
    private Mock<IOrderReportService> _orderReportServiceMock = null!;
    private Mock<IOrderService> _orderServiceMock = null!;
    private Mock<IPermissionService> _permissionServiceMock = null!;
    private Mock<IReportDataScope> _scopeMock = null!;
    private Mock<ICustomerReportViewModelService> _customerReportViewModelServiceMock = null!;
    private Mock<ICustomerReportService> _customerReportServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _orderReportServiceMock = new Mock<IOrderReportService>();
        var productsReportServiceMock = new Mock<IProductsReportService>();
        _customerReportViewModelServiceMock = new Mock<ICustomerReportViewModelService>();
        var priceFormatterMock = new Mock<IPriceFormatter>();
        priceFormatterMock.Setup(p => p.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>())).Returns("$0.00");
        var currencyServiceMock = new Mock<ICurrencyService>();
        currencyServiceMock.Setup(c => c.GetPrimaryStoreCurrency()).ReturnsAsync(new Currency());
        var productServiceMock = new Mock<IProductService>();
        var productAttributeFormatterMock = new Mock<IProductAttributeFormatter>();
        var stockQuantityServiceMock = new Mock<IStockQuantityService>();
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        var storeServiceMock = new Mock<IStoreService>();
        var countryServiceMock = new Mock<ICountryService>();
        var vendorServiceMock = new Mock<IVendorService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        var orderStatusServiceMock = new Mock<IOrderStatusService>();
        orderStatusServiceMock.Setup(o => o.GetAll()).ReturnsAsync(new List<Grand.Domain.Orders.OrderStatus>());
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        var contextAccessorMock = new Mock<IContextAccessor>();
        _scopeMock = new Mock<IReportDataScope>();
        _scopeMock.Setup(s => s.StoreId).Returns("");
        _scopeMock.Setup(s => s.VendorId).Returns("");
        _orderServiceMock = new Mock<IOrderService>();
        _customerReportServiceMock = new Mock<ICustomerReportService>();
        _permissionServiceMock = new Mock<IPermissionService>();
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageOrders)).ReturnsAsync(true);

        _controller = new TestFullReportsController(_orderReportServiceMock.Object, productsReportServiceMock.Object,
            _customerReportViewModelServiceMock.Object, priceFormatterMock.Object, currencyServiceMock.Object,
            productServiceMock.Object, productAttributeFormatterMock.Object, stockQuantityServiceMock.Object,
            translationServiceMock.Object, storeServiceMock.Object, countryServiceMock.Object,
            vendorServiceMock.Object, dateTimeServiceMock.Object, orderStatusServiceMock.Object,
            enumTranslationServiceMock.Object, contextAccessorMock.Object, _scopeMock.Object,
            _orderServiceMock.Object, _customerReportServiceMock.Object, _permissionServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        var routeData = new RouteData();
        routeData.Values["area"] = "Admin";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext, RouteData = routeData };
    }

    [TestMethod]
    public async Task BestsellersBriefReportByQuantityList_ManageOrdersDenied_ReturnsEmptyContent()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageOrders)).ReturnsAsync(false);

        var result = await _controller.BestsellersBriefReportByQuantityList(new DataSourceRequest { Page = 1, PageSize = 10 }) as ContentResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("", result!.Content);
        _orderReportServiceMock.Verify(o => o.BestSellersReport(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int?>(), It.IsAny<Grand.Domain.Payments.PaymentStatus?>(),
            It.IsAny<Grand.Domain.Shipping.ShippingStatus?>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task BestsellersBriefReportByAmountList_ManageOrdersAllowed_DelegatesToBase()
    {
        _orderReportServiceMock.Setup(o => o.BestSellersReport("", "", null, null, null, null, null, "", 2, 0, 10, true))
            .ReturnsAsync(new PagedList<BestsellersReportLine>(new List<BestsellersReportLine>(), 0, 0));

        var result = await _controller.BestsellersBriefReportByAmountList(new DataSourceRequest { Page = 1, PageSize = 10 });

        Assert.IsInstanceOfType(result, typeof(JsonResult));
    }

    [TestMethod]
    public async Task ReportOrderPeriodList_ManageOrdersDenied_ReturnsEmptyContent()
    {
        _permissionServiceMock.Setup(p => p.Authorize(StandardPermission.ManageOrders)).ReturnsAsync(false);

        var result = await _controller.ReportOrderPeriodList(new DataSourceRequest { Page = 1, PageSize = 10 }) as ContentResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("", result!.Content);
    }

    [TestMethod]
    public async Task ReportOrderTimeChart_ManageOrdersAllowed_PassesScopeStoreId()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _orderReportServiceMock.Setup(o => o.GetOrderByTimeReport("store-1", null, null))
            .ReturnsAsync(new List<OrderByTimeReportLine>());

        await _controller.ReportOrderTimeChart(new DataSourceRequest { Page = 1, PageSize = 10 }, null, null);

        _orderReportServiceMock.Verify(o => o.GetOrderByTimeReport("store-1", null, null), Times.Once);
    }

    [TestMethod]
    public async Task OrderAverageReportList_ManageOrdersAllowed_UsesScopeStoreIdForAllFourStatuses()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _orderReportServiceMock.Setup(o => o.OrderAverageReport("store-1", It.IsAny<int>()))
            .ReturnsAsync(new OrderAverageReportLineSummary());

        var result = await _controller.OrderAverageReportList(new DataSourceRequest { Page = 1, PageSize = 10 });

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        _orderReportServiceMock.Verify(o => o.OrderAverageReport("store-1", It.IsAny<int>()), Times.Exactly(4));
    }

    [TestMethod]
    public async Task ReportLatestOrder_ManageOrdersAllowed_PassesScopeStoreIdToSearchOrders()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _orderServiceMock.Setup(o => o.SearchOrders("store-1", "", "", "", "", "", "", "", "",
                null, null, null, null, null, null, null, "", null, null, 0, 10, ""))
            .ReturnsAsync(new PagedList<Order>(new List<Order>(), 0, 0));

        await _controller.ReportLatestOrder(new DataSourceRequest { Page = 1, PageSize = 10 }, null, null);

        _orderServiceMock.Verify(o => o.SearchOrders("store-1", "", "", "", "", "", "", "", "",
            null, null, null, null, null, null, null, "", null, null, 0, 10, ""), Times.Once);
    }

    [TestMethod]
    public async Task OrderIncompleteReportList_ManageOrdersAllowed_ReturnsThreeRows()
    {
        _orderReportServiceMock.Setup(o => o.GetOrderAverageReportLine(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<Grand.Domain.Payments.PaymentStatus?>(), It.IsAny<Grand.Domain.Shipping.ShippingStatus?>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<string>(), true, It.IsAny<string>()))
            .ReturnsAsync(new OrderAverageReportLine());

        var result = await _controller.OrderIncompleteReportList(new DataSourceRequest { Page = 1, PageSize = 10 }) as JsonResult;

        var gridModel = (DataSourceResult)result!.Value!;
        Assert.AreEqual(3, ((List<OrderIncompleteReportLineModel>)gridModel.Data).Count);
    }

    [TestMethod]
    public async Task ReportBestCustomersByNumberOfOrdersList_PassesScopeVendorIdToService()
    {
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _customerReportViewModelServiceMock.Setup(s =>
                s.PrepareBestCustomerReportLineModel(It.IsAny<BestCustomersReportModel>(), 2, 1, 10, "vendor-1"))
            .ReturnsAsync((new List<BestCustomerReportLineModel>(), 0));

        await _controller.ReportBestCustomersByNumberOfOrdersList(new DataSourceRequest { Page = 1, PageSize = 10 },
            new BestCustomersReportModel());

        _customerReportViewModelServiceMock.Verify(s =>
            s.PrepareBestCustomerReportLineModel(It.IsAny<BestCustomersReportModel>(), 2, 1, 10, "vendor-1"), Times.Once);
    }

    [TestMethod]
    public async Task ReportRegisteredCustomersList_PassesScopeStoreIdAndVendorIdToService()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _scopeMock.Setup(s => s.VendorId).Returns("vendor-1");
        _customerReportViewModelServiceMock.Setup(s => s.GetReportRegisteredCustomersModel("store-1", "vendor-1"))
            .ReturnsAsync(new List<RegisteredCustomerReportLineModel>());

        await _controller.ReportRegisteredCustomersList(new DataSourceRequest { Page = 1, PageSize = 10 });

        _customerReportViewModelServiceMock.Verify(s => s.GetReportRegisteredCustomersModel("store-1", "vendor-1"), Times.Once);
    }

    [TestMethod]
    public async Task ReportCustomerTimeChart_PassesScopeStoreIdToService()
    {
        _scopeMock.Setup(s => s.StoreId).Returns("store-1");
        _customerReportServiceMock.Setup(s => s.GetCustomerByTimeReport("store-1", null, null))
            .ReturnsAsync(new List<CustomerByTimeReportLine>());

        await _controller.ReportCustomerTimeChart(new DataSourceRequest { Page = 1, PageSize = 10 }, null, null);

        _customerReportServiceMock.Verify(s => s.GetCustomerByTimeReport("store-1", null, null), Times.Once);
    }
}
