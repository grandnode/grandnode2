using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.Common.DataSource;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseOnlineCustomerControllerTests
{
    // Concrete subclass exists only to instantiate the abstract base for direct-call unit tests -
    // same pattern as BaseTaxCategoryControllerTests/BaseEmailAccountControllerTests. Exposes the
    // protected SalesEmployeeIdFilter as a settable property so both the default (Store-shaped) and
    // overridden (Admin-shaped) branches can be exercised from one test class.
    private class TestOnlineCustomerController(
        ICustomerService customerService,
        IDateTimeService dateTimeService,
        CustomerSettings customerSettings,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
        : BaseOnlineCustomerController(customerService, dateTimeService, customerSettings, translationService, contextAccessor)
    {
        public string SalesEmployeeIdFilterOverride { get; set; }
        protected override string SalesEmployeeIdFilter => SalesEmployeeIdFilterOverride;
    }

    private Mock<ICustomerService> _customerServiceMock = null!;
    private Mock<IDateTimeService> _dateTimeServiceMock = null!;
    private Mock<ITranslationService> _translationServiceMock = null!;
    private Mock<IContextAccessor> _contextAccessorMock = null!;
    private CustomerSettings _customerSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _dateTimeServiceMock = new Mock<IDateTimeService>();
        _dateTimeServiceMock
            .Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime dt, DateTimeKind _) => dt);
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string s) => s);
        _customerSettings = new CustomerSettings { OnlineCustomerMinutes = 20, StoreLastVisitedPage = true };

        _contextAccessorMock = new Mock<IContextAccessor>();
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = "store-1", SeId = "se-1" });
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContext.Object);
    }

    private TestOnlineCustomerController CreateController(string salesEmployeeIdFilter = null)
    {
        return new TestOnlineCustomerController(
            _customerServiceMock.Object,
            _dateTimeServiceMock.Object,
            _customerSettings,
            _translationServiceMock.Object,
            _contextAccessorMock.Object) { SalesEmployeeIdFilterOverride = salesEmployeeIdFilter };
    }

    [TestMethod]
    public async Task List_DefaultFilter_PassesNullSalesEmployeeId()
    {
        _customerServiceMock
            .Setup(s => s.GetOnlineCustomers(It.IsAny<DateTime>(), null, "store-1", null, 0, 10))
            .ReturnsAsync(new PagedList<Customer>());
        var controller = CreateController();

        var result = await controller.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        var json = (JsonResult)result;
        var gridModel = (DataSourceResult)json.Value!;
        Assert.AreEqual(0, gridModel.Total);
        _customerServiceMock.Verify(s => s.GetOnlineCustomers(It.IsAny<DateTime>(), null, "store-1", null, 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task List_OverriddenFilter_PassesSalesEmployeeId()
    {
        _customerServiceMock
            .Setup(s => s.GetOnlineCustomers(It.IsAny<DateTime>(), null, "store-1", "se-1", 0, 10))
            .ReturnsAsync(new PagedList<Customer>());
        var controller = CreateController("se-1");

        await controller.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        _customerServiceMock.Verify(s => s.GetOnlineCustomers(It.IsAny<DateTime>(), null, "store-1", "se-1", 0, 10), Times.Once);
    }

    [TestMethod]
    public async Task List_MapsCustomersToModel_WithGuestFallbackAndLastVisitedPage()
    {
        var customers = new PagedList<Customer> {
            new() {
                Id = "c1", Email = "customer@example.com", LastIpAddress = "127.0.0.1",
                LastActivityDateUtc = new DateTime(2026, 1, 1), LastVisitedPage = "/home"
            },
            new() {
                Id = "c2", Email = "", LastIpAddress = "127.0.0.2",
                LastActivityDateUtc = new DateTime(2026, 1, 2), LastVisitedPage = "/cart"
            }
        };
        _customerServiceMock
            .Setup(s => s.GetOnlineCustomers(It.IsAny<DateTime>(), null, "store-1", null, 0, 10))
            .ReturnsAsync(customers);
        var controller = CreateController();

        var result = await controller.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        var gridModel = (DataSourceResult)((JsonResult)result).Value!;
        var items = (List<OnlineCustomerModel>)gridModel.Data;
        Assert.AreEqual(2, items.Count);
        Assert.AreEqual("customer@example.com", items[0].CustomerInfo);
        Assert.AreEqual("Admin.Customers.Guest", items[1].CustomerInfo);
        Assert.AreEqual("/home", items[0].LastVisitedPage);
    }

    [TestMethod]
    public async Task List_StoreLastVisitedPageDisabled_ReturnsDisabledResource()
    {
        _customerSettings.StoreLastVisitedPage = false;
        var customers = new PagedList<Customer> {
            new() { Id = "c1", Email = "customer@example.com", LastVisitedPage = "/home" }
        };
        _customerServiceMock
            .Setup(s => s.GetOnlineCustomers(It.IsAny<DateTime>(), null, "store-1", null, 0, 10))
            .ReturnsAsync(customers);
        var controller = CreateController();

        var result = await controller.List(new DataSourceRequest { Page = 1, PageSize = 10 });

        var gridModel = (DataSourceResult)((JsonResult)result).Value!;
        var items = (List<OnlineCustomerModel>)gridModel.Data;
        Assert.AreEqual("Admin.Dashboards.OnlineCustomers.Fields.LastVisitedPage.Disabled", items[0].LastVisitedPage);
    }
}
