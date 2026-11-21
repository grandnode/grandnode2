using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class RecurringPaymentControllerTests
{
    private Customer _customer;
    private CustomerSettings _customerSettings;
    private Grand.Domain.Stores.Store _store;
    private Mock<IMediator> _mediator;
    private Mock<IRecurringPaymentService> _recurringPaymentService;
    private RecurringPaymentController _controller;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "buyer", Email = "buyer@shop.test" };
        _store = new Grand.Domain.Stores.Store { Id = "s1" };

        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(() => _customer);
        work.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "en" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(s => s.CurrentStore).Returns(() => _store);
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(storeContext.Object);

        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);

        _mediator = new Mock<IMediator>();
        _mediator.Setup(m => m.Send(It.IsAny<GetRecurringPayments>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RecurringPaymentListModel());
        _recurringPaymentService = new Mock<IRecurringPaymentService>();
        _customerSettings = new CustomerSettings();
        _controller = new RecurringPaymentController(context.Object, _mediator.Object,
            _recurringPaymentService.Object, translation.Object, _customerSettings,
            new Mock<ILogger<RecurringPaymentController>>().Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, new Mock<ITempDataProvider>().Object);
    }

    private static void AssertRedirectTo(IActionResult result, string routeName)
    {
        Assert.IsInstanceOfType<RedirectToRouteResult>(result);
        Assert.AreEqual(routeName, ((RedirectToRouteResult)result).RouteName);
    }

    private IList<string> Notifications(string type)
    {
        return _controller.TempData[$"grand.notifications.{type}"] as IList<string> ?? [];
    }

    [TestMethod]
    public async Task List_TabOff_RedirectsToCustomerInfo()
    {
        _customerSettings.HideRecurringPaymentsTab = true;

        var result = await _controller.List(new OrderPagingModel());

        AssertRedirectTo(result, "CustomerInfo");
        _mediator.Verify(m => m.Send(It.IsAny<GetRecurringPayments>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task List_SendsTheCurrentCustomerAndStore()
    {
        var result = await _controller.List(new OrderPagingModel());

        Assert.IsInstanceOfType<ViewResult>(result);
        _mediator.Verify(m => m.Send(
            It.Is<GetRecurringPayments>(q => q.Customer.Id == "buyer" && q.Store.Id == "s1" && q.Language.Id == "en"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    //cancelling

    private void Stored(RecurringPayment recurringPayment)
    {
        _recurringPaymentService.Setup(x => x.GetRecurringPaymentById(recurringPayment.Id))
            .ReturnsAsync(recurringPayment);
    }

    private void GatewayAnswers(params string[] errors)
    {
        var result = new CancelRecurringPaymentResult();
        foreach (var error in errors)
            result.AddError(error);
        _mediator.Setup(m => m.Send(It.IsAny<CancelRecurringPaymentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private void AssertNotCancelled()
    {
        _mediator.Verify(m => m.Send(It.IsAny<CancelRecurringPaymentCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Cancel_OwnPayment_SendsTheCommandWithTheCustomersEmail()
    {
        var recurringPayment = new RecurringPayment { Id = "rp1", CustomerId = "buyer", StoreId = "s1" };
        Stored(recurringPayment);
        GatewayAnswers();

        var result = await _controller.Cancel("rp1");

        AssertRedirectTo(result, "CustomerRecurringPayments");
        _mediator.Verify(m => m.Send(
            It.Is<CancelRecurringPaymentCommand>(c =>
                c.RecurringPayment == recurringPayment && c.CancelledBy == "buyer@shop.test"),
            It.IsAny<CancellationToken>()), Times.Once);
        CollectionAssert.Contains(Notifications("success").ToList(), "RecurringPayment.Cancel.Success");
    }

    [TestMethod]
    public async Task Cancel_OtherCustomersPayment_Refused()
    {
        Stored(new RecurringPayment { Id = "rp1", CustomerId = "someone-else", StoreId = "s1" });

        var result = await _controller.Cancel("rp1");

        AssertRedirectTo(result, "CustomerRecurringPayments");
        AssertNotCancelled();
        CollectionAssert.Contains(Notifications("error").ToList(), "RecurringPayment.Cancel.Refused");
    }

    [TestMethod]
    public async Task Cancel_OtherStoresPayment_Refused()
    {
        Stored(new RecurringPayment { Id = "rp1", CustomerId = "buyer", StoreId = "s2" });

        var result = await _controller.Cancel("rp1");

        AssertRedirectTo(result, "CustomerRecurringPayments");
        AssertNotCancelled();
        CollectionAssert.Contains(Notifications("error").ToList(), "RecurringPayment.Cancel.Refused");
    }

    [TestMethod]
    public async Task Cancel_UnknownId_Refused()
    {
        var result = await _controller.Cancel("missing");

        AssertRedirectTo(result, "CustomerRecurringPayments");
        AssertNotCancelled();
        CollectionAssert.Contains(Notifications("error").ToList(), "RecurringPayment.Cancel.Refused");
    }

    [TestMethod]
    public async Task Cancel_ProviderRefuses_ShowsTheGenericMessageNotTheProviderText()
    {
        Stored(new RecurringPayment { Id = "rp1", CustomerId = "buyer", StoreId = "s1" });
        GatewayAnswers("stripe: resource_missing");

        var result = await _controller.Cancel("rp1");

        AssertRedirectTo(result, "CustomerRecurringPayments");
        CollectionAssert.Contains(Notifications("error").ToList(), "RecurringPayment.Cancel.Failed");
        Assert.IsFalse(Notifications("error").Concat(Notifications("success")).Any(x => x.Contains("stripe")));
    }

    [TestMethod]
    public async Task Cancel_TabOff_RedirectsToCustomerInfo()
    {
        _customerSettings.HideRecurringPaymentsTab = true;
        Stored(new RecurringPayment { Id = "rp1", CustomerId = "buyer", StoreId = "s1" });

        var result = await _controller.Cancel("rp1");

        AssertRedirectTo(result, "CustomerInfo");
        _recurringPaymentService.Verify(x => x.GetRecurringPaymentById(It.IsAny<string>()), Times.Never);
        AssertNotCancelled();
    }
}
