using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
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
}
