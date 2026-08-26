using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Mediator;
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
        ITranslationService translationService,
        IDateTimeService dateTimeService,
        IMediator mediator,
        IEnumTranslationService enumTranslationService,
        IAdminDataScope<PaymentTransaction> scope)
        : BasePaymentTransactionController(paymentTransactionService, orderService, translationService,
            dateTimeService, mediator, enumTranslationService, scope);

    private TestPaymentTransactionController _controller;
    private Mock<IPaymentTransactionService> _paymentTransactionServiceMock;
    private Mock<IOrderService> _orderServiceMock;
    private Mock<IAdminDataScope<PaymentTransaction>> _scopeMock;
    // Declared as a field (not a local in Setup()) because later tasks in this plan (Task 3, Task
    // 4) add further .Setup() calls on it from their own test methods for Capture/Refund/Void/
    // partial-refund-and-paid commands.
    private Mock<IMediator> _mediatorMock;

    [TestInitialize]
    public void Setup()
    {
        _paymentTransactionServiceMock = new Mock<IPaymentTransactionService>();
        _orderServiceMock = new Mock<IOrderService>();
        _scopeMock = new Mock<IAdminDataScope<PaymentTransaction>>();
        _scopeMock.Setup(s => s.HasAccess(It.IsAny<PaymentTransaction>())).ReturnsAsync(true);

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");
        var dateTimeServiceMock = new Mock<IDateTimeService>();
        dateTimeServiceMock.Setup(d => d.ConvertToUserTime(It.IsAny<DateTime>(), It.IsAny<DateTimeKind>()))
            .Returns((DateTime dt, DateTimeKind _) => dt);
        _mediatorMock = new Mock<IMediator>();
        var enumTranslationServiceMock = new Mock<IEnumTranslationService>();
        // ToSelectList<TEnum> is generic with a `where TEnum : struct` constraint, so the setup must
        // target the concrete enum actually passed at the List() call site (TransactionStatus), not
        // the abstract System.Enum base class - Enum doesn't satisfy `struct` and won't compile as a
        // type argument here. The three-argument overload (with the optional valuesToExclude) is the
        // one the compiler binds to, so the setup must match all three.
        enumTranslationServiceMock
            .Setup(e => e.ToSelectList(It.IsAny<TransactionStatus>(), It.IsAny<bool>(), It.IsAny<int[]>()))
            .Returns(new SelectList(new List<SelectListItem>()));

        _controller = new TestPaymentTransactionController(
            _paymentTransactionServiceMock.Object, _orderServiceMock.Object, translationServiceMock.Object,
            dateTimeServiceMock.Object, _mediatorMock.Object, enumTranslationServiceMock.Object, _scopeMock.Object);

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
        _paymentTransactionServiceMock
            .Setup(s => s.SearchPaymentTransactions(null, null, null, null, 0, int.MaxValue, null, null))
            .ReturnsAsync(new PagedList<PaymentTransaction>(new List<PaymentTransaction>(), 0, 10));

        var model = new PaymentTransactionListModel();
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = int.MaxValue }, model);

        Assert.IsNull(model.StoreId);
    }

    [TestMethod]
    public async Task List_Post_StoreScope_ForcesStoreId()
    {
        _scopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        _paymentTransactionServiceMock
            .Setup(s => s.SearchPaymentTransactions(null, "store-1", It.IsAny<string>(), null, 0, int.MaxValue, null, null))
            .ReturnsAsync(new PagedList<PaymentTransaction>(new List<PaymentTransaction>(), 0, 10));

        var model = new PaymentTransactionListModel { StoreId = "some-other-store" };
        await _controller.List(new DataSourceRequest { Page = 1, PageSize = int.MaxValue }, model);

        Assert.AreEqual("store-1", model.StoreId);
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

    [TestMethod]
    public async Task Delete_Authorized_DeletesAndRedirectsToList()
    {
        var transaction = new PaymentTransaction { Id = "pt-1" };
        _paymentTransactionServiceMock.Setup(s => s.GetById("pt-1")).ReturnsAsync(transaction);

        var result = await _controller.Delete("pt-1") as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("List", result!.ActionName);
        _paymentTransactionServiceMock.Verify(s => s.DeletePaymentTransaction(transaction), Times.Once);
    }
}
