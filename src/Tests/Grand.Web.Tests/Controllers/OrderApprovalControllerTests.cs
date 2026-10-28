using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Controllers;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class OrderApprovalControllerTests
{
    private Customer _customer;
    private CustomerSettings _customerSettings;
    private Grand.Domain.Stores.Store _store;
    private Currency _currency;
    private Mock<IMediator> _mediator;
    private OrderApprovalController _controller;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "buyer", OwnerId = "owner" };
        _store = new Grand.Domain.Stores.Store { Id = "s1" };
        _currency = new Currency { Id = "c1", CurrencyCode = "EUR" };

        var work = new Mock<IWorkContext>();
        work.Setup(w => w.CurrentCustomer).Returns(() => _customer);
        work.Setup(w => w.WorkingLanguage).Returns(new Language { Id = "en" });
        work.Setup(w => w.WorkingCurrency).Returns(() => _currency);
        work.Setup(w => w.TaxDisplayType).Returns(TaxDisplayType.ExcludingTax);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(s => s.CurrentStore).Returns(() => _store);
        storeContext.Setup(s => s.CurrentHost).Returns(new Grand.Domain.Stores.DomainHost { HostName = "shop.test" });
        var context = new Mock<IContextAccessor>();
        context.Setup(c => c.WorkContext).Returns(work.Object);
        context.Setup(c => c.StoreContext).Returns(storeContext.Object);

        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);

        _mediator = new Mock<IMediator>();
        _customerSettings = new CustomerSettings();
        _controller = new OrderApprovalController(context.Object, _mediator.Object, translation.Object,
            _customerSettings) {
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

    //the feature switch

    [TestMethod]
    public async Task List_FeatureOff_RedirectsToCustomerInfo()
    {
        _customerSettings.HideSubAccountsTab = true;

        var result = await _controller.List(new OrderPagingModel());

        AssertRedirectTo(result, "CustomerInfo");
        _mediator.Verify(m => m.Send(It.IsAny<GetOrderApprovals>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task EveryAction_FeatureOff_RedirectsToCustomerInfoAndDoesNothing()
    {
        _customerSettings.HideSubAccountsTab = true;

        AssertRedirectTo(await _controller.Details("a1"), "CustomerInfo");
        AssertRedirectTo(await _controller.Decide("a1", true, null), "CustomerInfo");
        AssertRedirectTo(await _controller.Withdraw("a1"), "CustomerInfo");

        _mediator.Verify(m => m.Send(It.IsAny<DecideOrderApprovalCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(m => m.Send(It.IsAny<WithdrawOrderApprovalCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mediator.Verify(m => m.Send(It.IsAny<GetOrderApproval>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    //list

    [TestMethod]
    public async Task List_FeatureOn_ReturnsTheModel()
    {
        var model = new OrderApprovalListModel();
        _mediator.Setup(m => m.Send(It.IsAny<GetOrderApprovals>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.List(new OrderPagingModel { PageNumber = 2 });

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreSame(model, ((ViewResult)result).Model);
        _mediator.Verify(m => m.Send(It.Is<GetOrderApprovals>(q =>
            q.Customer == _customer && q.Store == _store && q.Command.PageNumber == 2), It.IsAny<CancellationToken>()));
    }

    //details

    [TestMethod]
    public async Task Details_OtherCompany_RedirectsToList()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetOrderApproval>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderApprovalModel)null);

        var result = await _controller.Details("foreign");

        AssertRedirectTo(result, "CustomerOrderApprovals");
        _mediator.Verify(m => m.Send(It.Is<GetOrderApproval>(q =>
            q.Id == "foreign" && q.Customer == _customer && q.Store == _store), It.IsAny<CancellationToken>()));
    }

    [TestMethod]
    public async Task Details_Found_ReturnsTheModel()
    {
        var model = new OrderApprovalModel { Id = "a1" };
        _mediator.Setup(m => m.Send(It.IsAny<GetOrderApproval>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(model);

        var result = await _controller.Details("a1");

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.AreSame(model, ((ViewResult)result).Model);
    }

    //decide

    [TestMethod]
    public async Task Decide_Refused_ShowsError()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DecideOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Decide("a1", true, "ok");

        AssertRedirectTo(result, "CustomerOrderApprovals");
        CollectionAssert.AreEqual(new[] { "OrderApproval.Decide.Refused" }, Notifications("Error").ToArray());
        Assert.AreEqual(0, Notifications("Success").Count);
    }

    [TestMethod]
    public async Task Decide_Approved_SendsTheDecisionOfTheCurrentCustomer()
    {
        DecideOrderApprovalCommand sent = null;
        _mediator.Setup(m => m.Send(It.IsAny<DecideOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((c, _) => sent = (DecideOrderApprovalCommand)c)
            .ReturnsAsync(true);

        var result = await _controller.Decide("a1", true, "go ahead");

        AssertRedirectTo(result, "CustomerOrderApprovals");
        CollectionAssert.AreEqual(new[] { "OrderApproval.Decide.Approved" }, Notifications("Success").ToArray());
        Assert.AreSame(_customer, sent.Approver);
        Assert.AreSame(_store, sent.Store);
        Assert.AreEqual("a1", sent.ApprovalId);
        Assert.IsTrue(sent.Approve);
        Assert.AreEqual("go ahead", sent.Comment);
        Assert.AreEqual("shop.test", sent.Host.HostName);
        Assert.AreEqual("en", sent.Language.Id);
    }

    [TestMethod]
    public async Task Decide_Rejected_SaysRejected()
    {
        _mediator.Setup(m => m.Send(It.IsAny<DecideOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _controller.Decide("a1", false, null);

        CollectionAssert.AreEqual(new[] { "OrderApproval.Decide.Rejected" }, Notifications("Success").ToArray());
        _mediator.Verify(m => m.Send(It.Is<DecideOrderApprovalCommand>(c => !c.Approve),
            It.IsAny<CancellationToken>()));
    }

    //withdraw

    [TestMethod]
    public async Task Withdraw_SendsTheCurrentCustomersCommand()
    {
        WithdrawOrderApprovalCommand sent = null;
        _mediator.Setup(m => m.Send(It.IsAny<WithdrawOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<bool>, CancellationToken>((c, _) => sent = (WithdrawOrderApprovalCommand)c)
            .ReturnsAsync(true);

        var result = await _controller.Withdraw("a1");

        AssertRedirectTo(result, "CustomerOrderApprovals");
        Assert.AreEqual(1, Notifications("Success").Count);
        Assert.AreSame(_customer, sent.Customer);
        Assert.AreSame(_store, sent.Store);
        Assert.AreEqual("a1", sent.ApprovalId);
    }

    [TestMethod]
    public async Task Decide_WithoutApproveValue_IsRefusedAndNothingIsSent()
    {
        var result = await _controller.Decide("a1", null, "ok");

        AssertRedirectTo(result, "CustomerOrderApprovals");
        CollectionAssert.AreEqual(new[] { "OrderApproval.Decide.Refused" }, Notifications("Error").ToArray());
        _mediator.Verify(m => m.Send(It.IsAny<DecideOrderApprovalCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task Withdraw_ReturnsToTheList()
    {
        _mediator.Setup(m => m.Send(It.IsAny<WithdrawOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        AssertRedirectTo(await _controller.Withdraw("a1"), "CustomerOrderApprovals");
    }

    [TestMethod]
    public async Task Withdraw_Refused_ShowsError()
    {
        _mediator.Setup(m => m.Send(It.IsAny<WithdrawOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.Withdraw("a1");

        AssertRedirectTo(result, "CustomerOrderApprovals");
        CollectionAssert.AreEqual(new[] { "OrderApproval.Withdraw.Refused" }, Notifications("Error").ToArray());
    }
}
