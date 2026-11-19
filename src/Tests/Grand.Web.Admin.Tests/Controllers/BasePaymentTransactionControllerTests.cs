using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.SharedKernel;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BasePaymentTransactionControllerTests
{
    private class TestPaymentTransactionController(
        IPaymentTransactionService paymentTransactionService,
        IOrderService orderService,
        IRequestTranslationService translationService,
        IDateTimeService dateTimeService,
        IMediator mediator,
        IEnumTranslationService enumTranslationService,
        IAdminDataScope<PaymentTransaction> scope,
        IContextAccessor contextAccessor,
        IPaymentTransactionHistoryService paymentTransactionHistoryService)
        : BasePaymentTransactionController(paymentTransactionService, orderService, translationService,
            dateTimeService, mediator, enumTranslationService, scope, contextAccessor,
            paymentTransactionHistoryService);

    private TestPaymentTransactionController _controller;
    private Mock<IPaymentTransactionHistoryService> _historyMock;
    private Mock<IPaymentTransactionService> _paymentTransactionServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IAdminDataScope<PaymentTransaction>> _scopeMock;
    private Mock<IRequestTranslationService> _translationServiceMock;
    // Declared as a field (not a local in Setup()) because later tasks in this plan (Task 3, Task
    // 4) add further .Setup() calls on it from their own test methods for Capture/Refund/Void/
    // partial-refund-and-paid commands.
    private Mock<IMediator> _mediatorMock;
    private Mock<IEnumTranslationService> _enumTranslationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _paymentTransactionServiceMock = new Mock<IPaymentTransactionService>();
        _orderServiceMock = new Mock<IOrderService>();
        _scopeMock = new Mock<IAdminDataScope<PaymentTransaction>>();
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(true);

        _translationServiceMock = new Mock<IRequestTranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime dt, DateTimeKind _) => dt);
        _mediatorMock = new Mock<IMediator>();
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        _enumTranslationServiceMock = enumTranslationServiceMock;
        // ToSelectList<TEnum> is generic with a `where TEnum : struct` constraint, so the setup must
        // target the concrete enum actually passed at the List() call site (TransactionStatus), not
        // the abstract System.Enum base class - Enum doesn't satisfy `struct` and won't compile as a
        // type argument here. The three-argument overload (with the optional valuesToExclude) is the
        // one the compiler binds to, so the setup must match all three.
        enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<TransactionStatus>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(new List<SelectListItem>()));

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { Id = "staff", Email = "staff@shop.com" });
        var contextAccessorMock = new Mock<IContextAccessor>();
        contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _historyMock = new Mock<IPaymentTransactionHistoryService>();
        _historyMock.Setup(h => h.GetEntries(It.IsAny<string>())).ReturnsAsync(new List<PaymentTransactionEntry>());

        _controller = new TestPaymentTransactionController(
            _paymentTransactionServiceMock.Object, _orderServiceMock.Object, _translationServiceMock.Object,
            dateTimeServiceMock.Object, _mediatorMock.Object, enumTranslationServiceMock.Object, _scopeMock.Object,
            contextAccessorMock.Object, _historyMock.Object);

        // Error()/Success() (used by this task's popup and Delete actions) reach into
        // HttpContext.RequestServices for ILoggerFactory and into TempData - wire up the same
        // minimal harness other Base*ControllerTests use (see BaseOrderControllerTests.Setup).
        var httpContext = new DefaultHttpContext();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(l => l.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
        urlHelperFactoryMock.Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var requestServicesMock = new Mock<IServiceProvider>();
        requestServicesMock.Setup(s => s.GetService(typeof(ILoggerFactory))).Returns(loggerFactoryMock.Object);
        requestServicesMock.Setup(s => s.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactoryMock.Object);
        httpContext.RequestServices = requestServicesMock.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public void List_ReturnsViewWithPreparedModel()
    {
        var result = _controller.List() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result!.Model, typeof(PaymentTransactionListModel));
    }

    [TestMethod]
    public async Task List_Post_AdminScope_DoesNotForceStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns((string)null);
        SetupSearch();

        var model = new PaymentTransactionListModel();
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = int.MaxValue }, model);

        Assert.IsNull(model.StoreId);
    }

    private void SetupSearch()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPaymentTransactionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<PaymentTransaction>(new List<PaymentTransaction>(), 0, 10));
    }

    [TestMethod]
    public async Task List_Post_LeavesOutAbandonedUnlessAsked()
    {
        SetupSearch();
        var command = new DataSourceRequest { Page = 1, PageSize = int.MaxValue };

        await _controller.List(command, new PaymentTransactionListModel());
        await _controller.List(command, new PaymentTransactionListModel { ShowAbandoned = true });

        _mediatorMock.Verify(m => m.Send(It.Is<GetPaymentTransactionQuery>(q => !q.IncludeTemp), It.IsAny<CancellationToken>()), Times.Once);
        _mediatorMock.Verify(m => m.Send(It.Is<GetPaymentTransactionQuery>(q => q.IncludeTemp), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task List_Post_ByOrderNumber_IncludesAbandoned()
    {
        SetupSearch();
        var orderGuid = Guid.NewGuid();
        _orderServiceMock.Setup(s => s.GetOrderByNumber(1001)).ReturnsAsync(new Order { OrderGuid = orderGuid });

        await _controller.List(new DataSourceRequest { Page = 1, PageSize = int.MaxValue },
            new PaymentTransactionListModel { OrderNumber = "1001" });

        _mediatorMock.Verify(m => m.Send(It.Is<GetPaymentTransactionQuery>(q => q.OrderGuid == orderGuid && q.IncludeTemp),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task List_Post_StoreScope_ForcesStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        SetupSearch();

        var model = new PaymentTransactionListModel { StoreId = "some-other-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = int.MaxValue }, model);

        Assert.AreEqual("store-1", model.StoreId);
        _mediatorMock.Verify(m => m.Send(It.Is<GetPaymentTransactionQuery>(q => q.StoreId == "store-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task GoToOrderNumber_DeniedByScope_RedirectsToList()
    {
        _orderServiceMock.Setup(s => s.GetOrderByNumber(It.IsAny<int>())).ReturnsAsync(new Order { OrderGuid = Guid.NewGuid() });
        _paymentTransactionServiceMock.Setup(s => s.GetOrderByGuid(It.IsAny<Guid>()))
            .ReturnsAsync(new PaymentTransaction { Id = "pt-1" });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var result = await _controller.GoToOrderNumber(new PaymentTransactionListModel { OrderNumber = "123" }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task GoToOrderNumber_Authorized_RedirectsToEdit()
    {
        _orderServiceMock.Setup(s => s.GetOrderByNumber(It.IsAny<int>())).ReturnsAsync(new Order { OrderGuid = Guid.NewGuid() });
        _paymentTransactionServiceMock.Setup(s => s.GetOrderByGuid(It.IsAny<Guid>()))
            .ReturnsAsync(new PaymentTransaction { Id = "pt-1" });

        var result = await _controller.GoToOrderNumber(new PaymentTransactionListModel { OrderNumber = "123" }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result!.ActionName);
        Assert.AreEqual("pt-1", result!.RouteValues["id"]);
    }

    [TestMethod]
    public async Task Edit_DeniedByScope_RedirectsToList()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction { Id = "pt-1" });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var result = await _controller.Edit("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task Edit_Authorized_ReturnsViewWithModel()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", OrderGuid = Guid.NewGuid(), TransactionAmount = 100, RefundedAmount = 20, PaidAmount = 100 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _orderServiceMock.Setup(s => s.GetOrderByGuid(transaction.OrderGuid)).ReturnsAsync(new Order { Id = "order-1", OrderNumber = 42 });

        var result = await _controller.Edit("pt-1") as ViewResult;

        Assert.IsNotNull(result);
        var model = result!.Model as PaymentTransactionModel;
        Assert.IsNotNull(model);
        Assert.AreEqual("pt-1", model!.Id);
        Assert.AreEqual(80, model.MaxAmountToRefund);
    }

    [TestMethod]
    public async Task CapturePaymentTransaction_DeniedByScope_RedirectsToList()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction { Id = "pt-1" });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var result = await _controller.CapturePaymentTransaction("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
    }

    [TestMethod]
    public async Task CapturePaymentTransaction_Authorized_SendsCaptureCommandAndRedirectsToEdit()
    {
        var transaction = new PaymentTransaction { Id = "pt-1" };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<CaptureCommand>(), default)).ReturnsAsync(new List<string>());

        var result = await _controller.CapturePaymentTransaction("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result!.ActionName);
        Assert.AreEqual("pt-1", result!.RouteValues["id"]);
    }

    [TestMethod]
    public async Task PartiallyRefundPopup_Post_Success_SetsModelRefreshPage()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100, RefundedAmount = 0 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<PartiallyRefundOfflineCommand>(), default)).ReturnsAsync(true);

        var model = new PaymentTransactionModel { AmountToRefund = 50 };
        var result = await _controller.PartiallyRefundPopup("pt-1", false, model) as ViewResult;

        Assert.IsNotNull(result);
        var resultModel = result!.Model as PaymentTransactionModel;
        Assert.IsTrue(resultModel!.RefreshPage);
    }

    [TestMethod]
    public async Task PartiallyRefundPopup_Post_ZeroAmount_ErrorsAndDoesNotSetRefreshPage()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100, RefundedAmount = 0 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);

        var model = new PaymentTransactionModel { AmountToRefund = 0 };
        var result = await _controller.PartiallyRefundPopup("pt-1", false, model) as ViewResult;

        Assert.IsNotNull(result);
        var resultModel = result!.Model as PaymentTransactionModel;
        Assert.IsFalse(resultModel!.RefreshPage);
    }

    [TestMethod]
    public async Task PartiallyRefundPopup_Post_DeniedByScope_RedirectsToList()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction { Id = "pt-1" });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var model = new PaymentTransactionModel { AmountToRefund = 50 };
        var result = await _controller.PartiallyRefundPopup("pt-1", false, model) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<PartiallyRefundOfflineCommand>(), default), Times.Never);
    }

    private Order SetupOrder(string purchaseOrderNumber, DateTime? dueDate)
    {
        var orderGuid = Guid.NewGuid();
        var order = new Order {
            OrderGuid = orderGuid, PurchaseOrderNumber = purchaseOrderNumber, PaymentDueDateUtc = dueDate,
            PaymentStatusId = PaymentStatus.Pending, OrderStatusId = (int)OrderStatusSystem.Pending
        };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1"))
            .ReturnsAsync(new PaymentTransaction { Id = "pt-1", OrderGuid = orderGuid });
        _orderServiceMock.Setup(s => s.GetOrderByGuid(orderGuid)).ReturnsAsync(order);
        return order;
    }

    [TestMethod]
    public async Task Edit_OrderOnCredit_ShowsThePurchaseOrderNumberAndDueDate()
    {
        SetupOrder("PO-1", DateTime.UtcNow.Date.AddDays(-1));

        var result = await _controller.Edit("pt-1") as ViewResult;

        var model = (PaymentTransactionModel)result!.Model!;
        Assert.AreEqual("PO-1", model.PurchaseOrderNumber);
        Assert.AreEqual(DateTime.UtcNow.Date.AddDays(-1), model.PaymentDueDate);
        Assert.IsTrue(model.IsPaymentOverdue);
        Assert.IsTrue(model.BoughtOnCredit);
        Assert.IsTrue(model.CanEditPaymentTerms);
    }

    [TestMethod]
    public async Task Edit_OrderWithoutTerms_CannotEditThem()
    {
        SetupOrder(null, null);

        var result = await _controller.Edit("pt-1") as ViewResult;

        var model = (PaymentTransactionModel)result!.Model!;
        Assert.IsNull(model.PaymentDueDate);
        Assert.IsFalse(model.CanEditPaymentTerms);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Get_LoadsTheOrdersValues()
    {
        SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));

        var result = await _controller.PaymentTermsPopup("pt-1") as ViewResult;

        var model = (PaymentTransactionModel)result!.Model!;
        Assert.AreEqual("PO-1", model.PurchaseOrderNumber);
        Assert.AreEqual(new DateTime(2026, 11, 13), model.PaymentDueDate);
        Assert.IsTrue(model.BoughtOnCredit);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_SavesBothOnTheOrder()
    {
        var order = SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));

        var model = new PaymentTransactionModel { PurchaseOrderNumber = "  PO-2 ", PaymentDueDate = new DateTime(2026, 12, 1, 15, 30, 0) };
        var result = await _controller.PaymentTermsPopup("pt-1", model) as ViewResult;

        Assert.IsTrue(((PaymentTransactionModel)result!.Model!).RefreshPage);
        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(order, "PO-2",
            It.Is<DateTime?>(d => d == new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc) &&
                                  d.Value.Kind == DateTimeKind.Utc)), Times.Once);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_EmptyNumber_ClearsIt()
    {
        var order = SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));

        var model = new PaymentTransactionModel { PurchaseOrderNumber = " ", PaymentDueDate = new DateTime(2026, 11, 13) };
        await _controller.PaymentTermsPopup("pt-1", model);

        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(order, null, It.IsAny<DateTime?>()), Times.Once);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_TooLongNumber_NotSaved()
    {
        var order = SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));

        var model = new PaymentTransactionModel { PurchaseOrderNumber = new string('x', 51), PaymentDueDate = new DateTime(2026, 12, 1) };
        var result = await _controller.PaymentTermsPopup("pt-1", model) as ViewResult;

        Assert.IsFalse(_controller.ModelState.IsValid);
        Assert.IsFalse(((PaymentTransactionModel)result!.Model!).RefreshPage);
        Assert.AreEqual("PO-1", order.PurchaseOrderNumber);
        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_OrderOnCreditWithoutDueDate_NotSaved()
    {
        //clearing the date would silently take the order off the company's credit
        var order = SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));

        var result = await _controller.PaymentTermsPopup("pt-1", new PaymentTransactionModel { PurchaseOrderNumber = "PO-1" }) as ViewResult;

        Assert.IsFalse(_controller.ModelState.IsValid);
        Assert.IsTrue(((PaymentTransactionModel)result!.Model!).BoughtOnCredit);
        Assert.IsNotNull(order.PaymentDueDateUtc);
        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_UnreadableDueDate_ShowsTheSameMessageAsAnEmptyOne()
    {
        var order = SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));
        //what model binding leaves for text that is not a date: its message names the raw resource key
        _controller.ModelState.AddModelError(nameof(PaymentTransactionModel.PaymentDueDate), "The value 'x' is not valid for Admin.Orders.Fields.PaymentDueDate.");

        await _controller.PaymentTermsPopup("pt-1", new PaymentTransactionModel { PurchaseOrderNumber = "PO-1" });

        var errors = _controller.ModelState[nameof(PaymentTransactionModel.PaymentDueDate)]!.Errors;
        Assert.HasCount(1, errors);
        Assert.AreEqual("resource", errors[0].ErrorMessage);
        Assert.IsNotNull(order.PaymentDueDateUtc);
        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_OrderNotOnCredit_DueDateIgnored()
    {
        //a posted date must not put an order on the company's credit
        var order = SetupOrder("PO-1", null);

        var model = new PaymentTransactionModel { PurchaseOrderNumber = "PO-2", PaymentDueDate = new DateTime(2026, 12, 1) };
        await _controller.PaymentTermsPopup("pt-1", model);

        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(order, "PO-2", null), Times.Once);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_OrderWithoutTerms_NotSaved()
    {
        var order = SetupOrder(null, null);

        var model = new PaymentTransactionModel { PurchaseOrderNumber = "PO-2", PaymentDueDate = new DateTime(2026, 12, 1) };
        var result = await _controller.PaymentTermsPopup("pt-1", model) as RedirectToActionResult;

        Assert.AreEqual("Edit", result!.ActionName);
        Assert.IsNull(order.PurchaseOrderNumber);
        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [TestMethod]
    public async Task PaymentTermsPopup_Post_DeniedByScope_RedirectsToList()
    {
        SetupOrder("PO-1", new DateTime(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc));
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var model = new PaymentTransactionModel { PurchaseOrderNumber = "PO-2", PaymentDueDate = new DateTime(2026, 12, 1) };
        var result = await _controller.PaymentTermsPopup("pt-1", model) as RedirectToActionResult;

        Assert.AreEqual("List", result!.ActionName);
        _orderServiceMock.Verify(s => s.UpdatePaymentTerms(It.IsAny<Order>(), It.IsAny<string>(), It.IsAny<DateTime?>()), Times.Never);
    }

    [TestMethod]
    public async Task PartiallyPaidPopup_Post_Success_SetsModelRefreshPage()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100, PaidAmount = 0 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<PartiallyPaidOfflineCommand>(), default)).ReturnsAsync(true);

        var model = new PaymentTransactionModel { AmountToPaid = 50 };
        var result = await _controller.PartiallyPaidPopup("pt-1", false, model) as ViewResult;

        Assert.IsNotNull(result);
        var resultModel = result!.Model as PaymentTransactionModel;
        Assert.IsTrue(resultModel!.RefreshPage);
    }

    [TestMethod]
    public async Task PartiallyPaidPopup_Post_DeniedByScope_RedirectsToList()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction { Id = "pt-1" });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var model = new PaymentTransactionModel { AmountToPaid = 50 };
        var result = await _controller.PartiallyPaidPopup("pt-1", false, model) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
        _mediatorMock.Verify(m => m.Send(It.IsAny<PartiallyPaidOfflineCommand>(), default), Times.Never);
    }

    [TestMethod]
    public async Task Delete_DeniedByScope_RedirectsToList()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction { Id = "pt-1" });
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(false);

        var result = await _controller.Delete("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
    }

    private void SetupCanDelete(bool canDelete)
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<CanDeletePaymentTransactionQuery>(), default)).ReturnsAsync(canDelete);
    }

    [TestMethod]
    public async Task Delete_Allowed_DeletesAndRedirectsToList()
    {
        var transaction = new PaymentTransaction { Id = "pt-1" };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        SetupCanDelete(true);

        var result = await _controller.Delete("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
        _paymentTransactionServiceMock.Verify(s => s.DeletePaymentTransaction(transaction), Times.Once);
    }

    [TestMethod]
    public async Task Delete_Refused_ShowsWhyAndRedirectsToEdit()
    {
        SetupOrder(null, null);
        SetupCanDelete(false);

        var result = await _controller.Delete("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result!.ActionName);
        Assert.AreEqual("pt-1", result.RouteValues!["id"]);
        _translationServiceMock.Verify(t => t.GetResource("Admin.Orders.PaymentTransaction.CannotDelete"), Times.Once);
        _paymentTransactionServiceMock.Verify(s => s.DeletePaymentTransaction(It.IsAny<PaymentTransaction>()), Times.Never);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task Edit_CanDelete_FollowsTheRule(bool canDelete)
    {
        SetupOrder(null, null);
        SetupCanDelete(canDelete);

        var result = await _controller.Edit("pt-1") as ViewResult;

        Assert.AreEqual(canDelete, ((PaymentTransactionModel)result!.Model!).CanDelete);
    }

    [TestMethod]
    public async Task Edit_MapsTheRecordedHistoryInItsOrder()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", CurrencyCode = "EUR", TransactionAmount = 100 };
        _historyMock.Setup(h => h.GetEntries("pt-1")).ReturnsAsync(new List<PaymentTransactionEntry> {
            new() {
                EntryType = PaymentTransactionEntryType.Payment, Amount = 100, Reference = "TR-881", Note = "First",
                CreatedOnUtc = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new() {
                EntryType = PaymentTransactionEntryType.Refund, Amount = 10, RecordedBy = "staff@shop.com",
                CreatedOnUtc = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc)
            }
        });
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _enumTranslationServiceMock.Setup(e => e.GetTranslationEnum(PaymentTransactionEntryType.Payment))
            .Returns("Payment");
        _enumTranslationServiceMock.Setup(e => e.GetTranslationEnum(PaymentTransactionEntryType.Refund))
            .Returns("Refund");

        var model = (await _controller.Edit("pt-1") as ViewResult)!.Model as PaymentTransactionModel;

        Assert.HasCount(2, model!.Entries);
        Assert.AreEqual("Payment", model.Entries[0].Operation);
        Assert.AreEqual(100, model.Entries[0].Amount);
        Assert.AreEqual("TR-881", model.Entries[0].Reference);
        Assert.AreEqual("First", model.Entries[0].Note);
        Assert.AreEqual(new DateTime(2026, 11, 1), model.Entries[0].CreatedOn.Date);
        Assert.AreEqual("Refund", model.Entries[1].Operation);
        Assert.AreEqual("staff@shop.com", model.Entries[1].RecordedBy);
    }

    [TestMethod]
    public async Task Edit_WithoutEntries_HasEmptyList()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction { Id = "pt-1" });

        var model = (await _controller.Edit("pt-1") as ViewResult)!.Model as PaymentTransactionModel;

        Assert.IsNotNull(model!.Entries);
        Assert.IsEmpty(model.Entries);
    }

    [TestMethod]
    public async Task PartiallyPaidPopup_Post_SendsStaffReferenceAndNote()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<PartiallyPaidOfflineCommand>(), default)).ReturnsAsync(true);

        await _controller.PartiallyPaidPopup("pt-1", false,
            new PaymentTransactionModel { AmountToPaid = 50, Reference = "TR-881", Note = "First instalment" });

        _mediatorMock.Verify(m => m.Send(It.Is<PartiallyPaidOfflineCommand>(c =>
            c.CreatedBy == "staff@shop.com" && c.Reference == "TR-881" && c.Note == "First instalment" &&
            c.AmountToPaid == 50), default), Times.Once);
    }

    [TestMethod]
    public async Task PartiallyRefundPopup_Post_Offline_SendsStaffReferenceAndNote()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<PartiallyRefundOfflineCommand>(), default)).ReturnsAsync(true);

        await _controller.PartiallyRefundPopup("pt-1", false,
            new PaymentTransactionModel { AmountToRefund = 20, Reference = "RF-1", Note = "Damaged box" });

        _mediatorMock.Verify(m => m.Send(It.Is<PartiallyRefundOfflineCommand>(c =>
            c.CreatedBy == "staff@shop.com" && c.Reference == "RF-1" && c.Note == "Damaged box"), default), Times.Once);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task PartiallyRefundPopup_Get_CarriesWhetherTheRefundIsOnline(bool online)
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1"))
            .ReturnsAsync(new PaymentTransaction { Id = "pt-1", TransactionAmount = 100 });

        var model = (await _controller.PartiallyRefundPopup("pt-1", online) as ViewResult)!.Model as PaymentTransactionModel;

        Assert.AreEqual(online, model!.Online);
    }

    [TestMethod]
    public async Task MarkAsPaidPopup_Get_ShowsTheAmounts()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(new PaymentTransaction
            { Id = "pt-1", TransactionAmount = 100, PaidAmount = 40, CurrencyCode = "EUR" });

        var model = (await _controller.MarkAsPaidPopup("pt-1") as ViewResult)!.Model as PaymentTransactionModel;

        Assert.AreEqual(100, model!.TransactionAmount);
        Assert.AreEqual(40, model.PaidAmount);
        Assert.AreEqual("EUR", model.CurrencyCode);
    }

    [TestMethod]
    public async Task MarkAsPaidPopup_Post_SendsStaffReferenceAndNote()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<MarkAsPaidCommand>(), default)).ReturnsAsync(true);

        var result = await _controller.MarkAsPaidPopup("pt-1",
            new PaymentTransactionModel { Reference = "TR-881", Note = "Whole transfer" }) as ViewResult;

        Assert.IsTrue(((PaymentTransactionModel)result!.Model!).RefreshPage);
        _mediatorMock.Verify(m => m.Send(It.Is<MarkAsPaidCommand>(c =>
            c.PaymentTransaction == transaction && c.CreatedBy == "staff@shop.com" && c.Reference == "TR-881" &&
            c.Note == "Whole transfer"), default), Times.Once);
    }

    [TestMethod]
    public async Task MarkAsPaidPopup_Post_Refused_DoesNotRefresh()
    {
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1"))
            .ReturnsAsync(new PaymentTransaction { Id = "pt-1", TransactionAmount = 100 });
        _mediatorMock.Setup(m => m.Send(It.IsAny<MarkAsPaidCommand>(), default))
            .ThrowsAsync(new GrandException("You can't mark this Payment Transaction as paid"));

        var result = await _controller.MarkAsPaidPopup("pt-1", new PaymentTransactionModel()) as ViewResult;

        Assert.IsFalse(((PaymentTransactionModel)result!.Model!).RefreshPage);
    }

    [TestMethod]
    public async Task RefundOfflinePopup_Post_SendsStaffReferenceAndNote()
    {
        var transaction = new PaymentTransaction { Id = "pt-1", TransactionAmount = 100 };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);
        _mediatorMock.Setup(m => m.Send(It.IsAny<RefundOfflineCommand>(), default)).ReturnsAsync(true);

        var result = await _controller.RefundOfflinePopup("pt-1",
            new PaymentTransactionModel { Reference = "RF-9", Note = "By transfer" }) as ViewResult;

        Assert.IsTrue(((PaymentTransactionModel)result!.Model!).RefreshPage);
        _mediatorMock.Verify(m => m.Send(It.Is<RefundOfflineCommand>(c =>
            c.PaymentTransaction == transaction && c.CreatedBy == "staff@shop.com" && c.Reference == "RF-9" &&
            c.Note == "By transfer"), default), Times.Once);
    }
}
