using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

[TestClass]
public class OrderControllerWithdrawApprovalTests
{
    private readonly Customer _buyer = new() { Id = "buyer" };
    private OrderController _controller;
    private Mock<IMediator> _mediator;
    private Mock<IOrderService> _orders;
    private Mock<IRequestTranslationService> _translation;

    [TestInitialize]
    public void Init()
    {
        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(_buyer);
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Grand.Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        context.Setup(x => x.StoreContext).Returns(storeContext.Object);

        _orders = new Mock<IOrderService>();
        _mediator = new Mock<IMediator>();
        _translation = new Mock<IRequestTranslationService>();
        _translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns("text");

        //a customer without sub-accounts counts as an owner, as GroupService answers it
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.IsOwner(_buyer)).ReturnsAsync(true);

        _controller = new OrderController(_orders.Object, context.Object, new Mock<IPaymentService>().Object,
            new Mock<IPaymentTransactionService>().Object, _translation.Object, groupService.Object, _mediator.Object,
            new OrderSettings());

        var httpContext = new DefaultHttpContext();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new Mock<ILogger>().Object);
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>())).Returns(new Mock<IUrlHelper>().Object);
        var services = new Mock<IServiceProvider>();
        services.Setup(x => x.GetService(typeof(ILoggerFactory))).Returns(loggerFactory.Object);
        services.Setup(x => x.GetService(typeof(IUrlHelperFactory))).Returns(urlHelperFactory.Object);
        httpContext.RequestServices = services.Object;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private void Withdraws(bool answer)
    {
        _orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1", CustomerId = "buyer" });
        _mediator.Setup(x => x.Send(It.IsAny<WithdrawOrderApprovalCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);
    }

    [TestMethod]
    public async Task WithdrawApproval_NoOrderAccess_Challenges()
    {
        _orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(new Order { Id = "o1", CustomerId = "someone-else" });

        var result = await _controller.WithdrawApproval("o1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
        _mediator.Verify(x => x.Send(It.IsAny<WithdrawOrderApprovalCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task WithdrawApproval_RunsWithSubAccountsFeatureOff()
    {
        //the action has no CustomerSettings gate: a store-decided request exists precisely when the feature is off
        Withdraws(true);

        var result = await _controller.WithdrawApproval("o1");

        Assert.AreEqual("OrderDetails", ((RedirectToRouteResult)result).RouteName);
        _mediator.Verify(x => x.Send(It.Is<WithdrawOrderApprovalCommand>(c =>
            c.OrderId == "o1" && c.Customer == _buyer && c.Store.Id == "s1"), It.IsAny<CancellationToken>()), Times.Once);
        _translation.Verify(x => x.GetResource("Order.Approval.Withdrawn"), Times.Once);
    }

    [TestMethod]
    public async Task WithdrawApproval_Refused_ShowsTheError()
    {
        Withdraws(false);

        var result = await _controller.WithdrawApproval("o1");

        Assert.AreEqual("OrderDetails", ((RedirectToRouteResult)result).RouteName);
        _translation.Verify(x => x.GetResource("Order.Approval.WithdrawRefused"), Times.Once);
    }
}
