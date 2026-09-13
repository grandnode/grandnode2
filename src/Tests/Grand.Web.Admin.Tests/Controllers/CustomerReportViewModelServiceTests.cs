using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Business.Core.Utilities.System;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class CustomerReportViewModelServiceTests
{
    private CustomerReportViewModelService _service = null!;
    private Mock<ICustomerReportService> _customerReportServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        var customerServiceMock = new Mock<ICustomerService>();
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        _customerReportServiceMock = new Mock<ICustomerReportService>();
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        var priceFormatterMock = new Mock<Grand.Business.Core.Interfaces.Catalog.Prices.IPriceFormatter>();
        var orderStatusServiceMock = new Mock<IOrderStatusService>();
        var currencyServiceMock = new Mock<ICurrencyService>();
        currencyServiceMock.Setup(c => c.GetPrimaryStoreCurrency()).ReturnsAsync(new Currency());
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();

        _service = new CustomerReportViewModelService(customerServiceMock.Object, translationServiceMock.Object,
            _customerReportServiceMock.Object, dateTimeServiceMock.Object, priceFormatterMock.Object,
            orderStatusServiceMock.Object, currencyServiceMock.Object, enumTranslationServiceMock.Object);
    }

    [TestMethod]
    public async Task PrepareBestCustomerReportLineModel_DefaultVendorId_PassesEmptyStringToService()
    {
        _customerReportServiceMock.Setup(s => s.GetBestCustomersReport(It.IsAny<string>(), "", null, null, null, null, null, 2, 0, 10))
            .ReturnsAsync(new PagedList<BestCustomerReportLine>(new List<BestCustomerReportLine>(), 0, 0));

        await _service.PrepareBestCustomerReportLineModel(new BestCustomersReportModel(), 1, 1, 10);

        _customerReportServiceMock.Verify(s => s.GetBestCustomersReport(It.IsAny<string>(), "", null, null, null, null, null, 2, 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task PrepareBestCustomerReportLineModel_ExplicitVendorId_PassesItToService()
    {
        _customerReportServiceMock.Setup(s => s.GetBestCustomersReport(It.IsAny<string>(), "vendor-1", null, null, null, null, null, 2, 0, 10))
            .ReturnsAsync(new PagedList<BestCustomerReportLine>(new List<BestCustomerReportLine>(), 0, 0));

        await _service.PrepareBestCustomerReportLineModel(new BestCustomersReportModel(), 1, 1, 10, "vendor-1");

        _customerReportServiceMock.Verify(s => s.GetBestCustomersReport(It.IsAny<string>(), "vendor-1", null, null, null, null, null, 2, 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task GetReportRegisteredCustomersModel_DefaultVendorId_DoesNotThrow()
    {
        // GetRegisteredCustomersReport itself has no vendorId parameter (confirmed on
        // ICustomerReportService — registered-customer counts are never vendor-scoped in the business
        // layer); the new vendorId parameter on GetReportRegisteredCustomersModel exists purely for
        // Global Constraint 8's "both methods" symmetry (Task 9's header note) and is accepted but
        // not yet forwarded anywhere further. This test documents that "not forwarded" is intentional,
        // not a missed wire-up.
        _customerReportServiceMock.Setup(s => s.GetRegisteredCustomersReport("store-1", It.IsAny<int>())).ReturnsAsync(5);

        var result = await _service.GetReportRegisteredCustomersModel("store-1", "vendor-1");

        Assert.AreEqual(4, result.Count);
    }
}
