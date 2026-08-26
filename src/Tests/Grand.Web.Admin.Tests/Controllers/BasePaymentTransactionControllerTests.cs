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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    }

    [TestMethod]
    public void List_ReturnsViewWithPreparedModel()
    {
        var result = _controller.List() as ViewResult;

        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result.Model, typeof(PaymentTransactionListModel));
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
        Assert.AreEqual("List", result.ActionName);
    }

    [TestMethod]
    public async Task GoToOrderNumber_Authorized_RedirectsToEdit()
    {
        _orderServiceMock.Setup(s => s.GetOrderByNumber(It.IsAny<int>())).ReturnsAsync(new Order { OrderGuid = Guid.NewGuid() });
        _paymentTransactionServiceMock.Setup(s => s.GetOrderByGuid(It.IsAny<Guid>()))
            .ReturnsAsync(new PaymentTransaction { Id = "pt-1" });

        var result = await _controller.GoToOrderNumber(new PaymentTransactionListModel { OrderNumber = "123" }) as RedirectToActionResult;

        Assert.IsNotNull(result);
        Assert.AreEqual("Edit", result.ActionName);
        Assert.AreEqual("pt-1", result.RouteValues["id"]);
    }
}
