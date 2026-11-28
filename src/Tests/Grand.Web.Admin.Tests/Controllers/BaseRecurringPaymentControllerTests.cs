using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseRecurringPaymentControllerTests
{
    private class TestRecurringPaymentController(
        IRecurringPaymentService recurringPaymentService,
        IOrderService orderService,
        ICustomerService customerService,
        IStoreService storeService,
        IPaymentService paymentService,
        IRequestTranslationService translationService,
        IEnumTranslationService enumTranslationService,
        IDateTimeService dateTimeService,
        IMediator mediator,
        IAdminDataScope<Order> scope,
        IContextAccessor contextAccessor)
        : BaseRecurringPaymentController(recurringPaymentService, orderService, customerService, storeService,
            paymentService, translationService, enumTranslationService, dateTimeService, mediator, scope,
            contextAccessor);

    private TestRecurringPaymentController _controller;
    private Customer _staff;
    private Mock<IMediator> _mediator;
    private Order _initialOrder;
    private RecurringPayment _recurringPayment;
    private Mock<IAdminDataScope<Order>> _scope;

    public BaseRecurringPaymentControllerTests()
    {
        _initialOrder = new Order { Id = "o1", OrderNumber = 1042, StoreId = "s1", CustomerCurrencyCode = "USD" };
        _recurringPayment = new RecurringPayment {
            Id = "rp1", StoreId = "s1", CustomerId = "c1", InitialOrderId = "o1",
            Status = RecurringPaymentStatus.Active, CycleLength = 1, CyclePeriodId = Domain.Catalog.RecurringCyclePeriod.Months,
            Cycles = [new RecurringPaymentCycle { CycleNumber = 2, OrderId = "o2", Amount = 9.5 }]
        };

        var recurringPaymentService = new Mock<IRecurringPaymentService>();
        recurringPaymentService.Setup(x => x.GetRecurringPaymentById("rp1")).ReturnsAsync(() => _recurringPayment);
        var orderService = new Mock<IOrderService>();
        orderService.Setup(x => x.GetOrderById("o1")).ReturnsAsync(() => _initialOrder);
        orderService.Setup(x => x.GetOrdersByIds(It.IsAny<string[]>())).ReturnsAsync((string[] ids) =>
            new[] { _initialOrder, new Order { Id = "o2", OrderNumber = 1043 } }.Where(x => ids.Contains(x.Id)).ToList());
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(x => x.GetCustomerById("c1")).ReturnsAsync(new Customer { Id = "c1", Email = "jan@example.com" });
        customerService.Setup(x => x.GetCustomersByIds(It.IsAny<string[]>()))
            .ReturnsAsync(new List<Customer> { new() { Id = "c1", Email = "jan@example.com" } });
        var storeService = new Mock<IStoreService>();
        storeService.Setup(x => x.GetAllStores()).ReturnsAsync(new List<Store> { new() { Id = "s1", Shortcut = "Main" } });
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);
        var dateTimeService = new Mock<IDateTimeService>();
        dateTimeService.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime dt, DateTimeKind _) => dt);

        _mediator = new Mock<IMediator>();
        _mediator.Setup(x => x.Send(It.IsAny<GetRecurringPaymentsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<RecurringPayment>([_recurringPayment], 0, 15));
        _mediator.Setup(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CancelRecurringPaymentResult());

        _scope = new Mock<IAdminDataScope<Order>>();
        _scope.Setup(x => x.HasAccess(It.IsAny<Order>())).ReturnsAsync(true);

        _staff = new Customer { Id = "staff", Email = "staff@shop.com" };
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(w => w.CurrentCustomer).Returns(() => _staff);
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.WorkContext).Returns(workContext.Object);

        _controller = new TestRecurringPaymentController(recurringPaymentService.Object, orderService.Object,
            customerService.Object, storeService.Object, new Mock<IPaymentService>().Object, translationService.Object,
            new Mock<IEnumTranslationService>().Object, dateTimeService.Object, _mediator.Object, _scope.Object,
            contextAccessor.Object);

        var httpContext = new DefaultHttpContext();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServices = new Mock<IServiceProvider>();
        requestServices.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactory.Object);
        requestServices.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactory.Object);
        httpContext.RequestServices = requestServices.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task List_StorePanel_SearchesItsOwnStoreWhateverIsPosted()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("s1");

        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 15 },
            new RecurringPaymentListModel { StoreId = "s2" });

        _mediator.Verify(x => x.Send(It.Is<GetRecurringPaymentsQuery>(q => q.StoreId == "s1"),
            It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task List_Admin_SearchesThePostedStoreAndStatus()
    {
        await _controller.List(new DataSourceRequest { Page = 2, PageSize = 15 },
            new RecurringPaymentListModel { StoreId = " s2 ", SearchStatusId = 20, SearchCustomerEmail = "jan@example.com" });

        _mediator.Verify(x => x.Send(It.Is<GetRecurringPaymentsQuery>(q =>
            q.StoreId == "s2" && q.Status == RecurringPaymentStatus.PastDue && q.CustomerEmail == "jan@example.com" &&
            q.PageIndex == 1), It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task List_SalesEmployee_SearchesTheirOwnOrdersOnly()
    {
        _staff.SeId = "se1";

        await _controller.List(new DataSourceRequest { Page = 1, PageSize = 15 }, new RecurringPaymentListModel());

        _mediator.Verify(x => x.Send(It.Is<GetRecurringPaymentsQuery>(q => q.SalesEmployeeId == "se1"),
            It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task List_MapsTheRows()
    {
        var result = (JsonResult)await _controller.List(new DataSourceRequest { Page = 1, PageSize = 15 },
            new RecurringPaymentListModel());

        var row = Assert.IsInstanceOfType<IEnumerable<RecurringPaymentModel>>(((DataSourceResult)result.Value!).Data).Single();
        Assert.AreEqual("jan@example.com", row.CustomerEmail);
        Assert.AreEqual(1042, row.InitialOrderNumber);
        Assert.AreEqual("Main", row.StoreName);
        Assert.AreEqual(RecurringCycleText.ResourceKey(1, Domain.Catalog.RecurringCyclePeriod.Months), row.Cycle);
        Assert.IsTrue(row.CanCancel);
    }

    [TestMethod]
    public async Task Edit_ListsTheCyclesWithTheirOrders()
    {
        var model = (RecurringPaymentModel)((ViewResult)await _controller.Edit("rp1")).Model!;

        var cycle = model.Cycles.Single();
        Assert.AreEqual("o2", cycle.OrderId);
        Assert.AreEqual(1043, cycle.OrderNumber);
        Assert.IsTrue(cycle.CanViewOrder);
        StringAssert.EndsWith(cycle.Amount, "USD");
    }

    [TestMethod]
    public async Task Edit_StorePanel_OtherStore_RedirectsToList()
    {
        _scope.Setup(x => x.DefaultStoreId).Returns("s2");

        var result = await _controller.Edit("rp1");

        Assert.AreEqual("List", ((RedirectToActionResult)result).ActionName);
    }

    [TestMethod]
    public async Task Edit_InitialOrderOutOfScope_RedirectsToList()
    {
        _scope.Setup(x => x.HasAccess(_initialOrder)).ReturnsAsync(false);

        var result = await _controller.Edit("rp1");

        Assert.AreEqual("List", ((RedirectToActionResult)result).ActionName);
    }

    [TestMethod]
    public async Task Cancel_OutOfScope_DoesNotCancel()
    {
        _scope.Setup(x => x.HasAccess(_initialOrder)).ReturnsAsync(false);

        await _controller.Cancel("rp1");

        _mediator.Verify(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Cancel_Ended_DoesNotCancel()
    {
        _recurringPayment.Status = RecurringPaymentStatus.Cancelled;

        await _controller.Cancel("rp1");

        _mediator.Verify(x => x.Send(It.IsAny<CancelRecurringPaymentCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Cancel_NamesTheStaffMember()
    {
        var result = await _controller.Cancel("rp1");

        _mediator.Verify(x => x.Send(
            It.Is<CancelRecurringPaymentCommand>(c => c.RecurringPayment == _recurringPayment &&
                                                      c.CancelledBy == "staff@shop.com"),
            It.IsAny<CancellationToken>()));
        Assert.AreEqual("Edit", ((RedirectToActionResult)result).ActionName);
    }
}
