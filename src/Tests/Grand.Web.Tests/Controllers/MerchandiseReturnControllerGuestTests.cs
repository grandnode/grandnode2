using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Controllers;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     A visitor holding the guest grant of order o1 acts on the returns of o1 as its customer, and on nothing else
/// </summary>
[TestClass]
public class MerchandiseReturnControllerGuestTests
{
    private readonly Customer _guest = new() { Id = "g1", Email = "guest@example.com" };
    private readonly Customer _visitor = new() { Id = "visitor" };
    private MerchandiseReturnController _controller;
    private Mock<IGroupService> _groups;
    private DefaultHttpContext _httpContext;
    private Mock<IMediator> _mediator;
    private readonly Order _order1 = new() { Id = "o1", CustomerId = "g1", StoreId = "s1" };
    private readonly Order _order2 = new() { Id = "o2", CustomerId = "g2", StoreId = "s1" };

    private readonly OrderSettings _orderSettings =
        new() { MerchandiseReturnsEnabled = true, AllowGuestOrderAccess = true };

    private IDataProtectionProvider _provider;
    private Mock<IMerchandiseReturnWorkflowService> _workflow;

    [TestInitialize]
    public void Init()
    {
        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(_visitor);
        work.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "l1" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        context.Setup(x => x.StoreContext).Returns(storeContext.Object);

        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order1);
        orders.Setup(x => x.GetOrderById("o2")).ReturnsAsync(_order2);
        var returns = new Mock<IMerchandiseReturnService>();
        returns.Setup(x => x.GetMerchandiseReturnById("r1"))
            .ReturnsAsync(new MerchandiseReturn { Id = "r1", OrderId = "o1", CustomerId = "g1" });
        returns.Setup(x => x.GetMerchandiseReturnById("r2"))
            .ReturnsAsync(new MerchandiseReturn { Id = "r2", OrderId = "o2", CustomerId = "g2" });
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(_guest);
        customers.Setup(x => x.GetCustomerById("g2")).ReturnsAsync(new Customer { Id = "g2" });

        _groups = new Mock<IGroupService>();
        _groups.Setup(x => x.IsOwner(It.IsAny<Customer>())).ReturnsAsync(true);
        _groups.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);
        _workflow = new Mock<IMerchandiseReturnWorkflowService>();
        _mediator = new Mock<IMediator>();
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnDetails>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MerchandiseReturnDetailsModel());
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MerchandiseReturnModel());
        _mediator.Setup(x => x.Send(It.IsAny<IsMerchandiseReturnAllowedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns("text");

        _provider = new EphemeralDataProtectionProvider();
        _httpContext = new DefaultHttpContext {
            RequestServices = new ServiceCollection().AddSingleton(_provider).AddSingleton(customers.Object)
                .AddSingleton(_orderSettings)
                .AddSingleton<ICookieOptionsFactory>(new CookieOptionsFactory(new SecurityConfig()))
                .BuildServiceProvider()
        };
        Grant(_order1);

        _controller = new MerchandiseReturnController(returns.Object, new Mock<IPickupPointService>().Object,
            orders.Object, context.Object, _groups.Object, translation.Object, _mediator.Object,
            new Mock<IMerchandiseReturnLabelService>().Object, new Mock<IDownloadService>().Object,
            new AddressSettings(), _orderSettings, _workflow.Object) {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
        _controller.TempData = new TempDataDictionary(_httpContext, new Mock<ITempDataProvider>().Object);
        //the services hold no routing: redirects get their URLs from a helper of their own
        _controller.Url = new Mock<IUrlHelper>().Object;
    }

    /// <summary>
    ///     The request carries the grant cookie of the order, as the browser sends it back
    /// </summary>
    private void Grant(Order order)
    {
        var granting = new DefaultHttpContext { RequestServices = _httpContext.RequestServices };
        GuestOrderGrant.Grant(granting, order);
        var header = granting.Response.Headers.SetCookie.ToString();
        _httpContext.Request.Headers.Cookie = header[..header.IndexOf(';')];
    }

    [TestMethod]
    public async Task Details_GrantForOrder_View()
    {
        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ViewResult>(result);
    }

    [TestMethod]
    public async Task Details_GrantForOtherOrder_Challenge()
    {
        var result = await _controller.MerchandiseReturnDetails("r2");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Cancel_GrantForOtherOrder_NotFound()
    {
        var result = await _controller.MerchandiseReturnCancel("r2");

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        _workflow.Verify(x => x.ChangeStatus(It.IsAny<string>(), It.IsAny<MerchandiseReturnStatus>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<MerchandiseReturn, bool>>()), Times.Never);
    }

    [TestMethod]
    public async Task AddNote_GrantForOtherOrder_NotFound()
    {
        var result = await _controller.MerchandiseReturnAddNote("r2", "Hello", null);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Cancel_Grant_ChangedByOrderCustomer()
    {
        await _controller.MerchandiseReturnCancel("r1");

        _workflow.Verify(x => x.ChangeStatus("r1", MerchandiseReturnStatus.Cancelled, null,
            "customer guest@example.com", It.IsAny<Func<MerchandiseReturn, bool>>()), Times.Once);
    }

    [TestMethod]
    public async Task AddNote_Grant_SentAsOrderCustomer()
    {
        await _controller.MerchandiseReturnAddNote("r1", "Hello", null);

        _mediator.Verify(x => x.Send(It.Is<InsertMerchandiseReturnNoteCommand>(c => c.Customer.Id == "g1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Submit_Grant_SavedWithOrderCustomer()
    {
        _mediator.Setup(x => x.Send(It.IsAny<MerchandiseReturnSubmitCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MerchandiseReturnSubmitResult { Error = "taken" });

        await _controller.MerchandiseReturn(new MerchandiseReturnModel { OrderId = "o1" });

        _mediator.Verify(x => x.Send(It.Is<MerchandiseReturnSubmitCommand>(c => c.Customer.Id == "g1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Wizard_Grant_ShownForOrderCustomer()
    {
        var result = await _controller.MerchandiseReturn("o1");

        Assert.IsInstanceOfType<ViewResult>(result);
        _mediator.Verify(x => x.Send(It.Is<GetMerchandiseReturn>(c => c.Customer.Id == "g1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task Wizard_GrantForOtherOrder_Challenge()
    {
        var result = await _controller.MerchandiseReturn("o2");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Details_GrantSettingOff_Challenge()
    {
        _orderSettings.AllowGuestOrderAccess = false;

        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Details_GrantReturnsDisabled_Challenge()
    {
        _orderSettings.MerchandiseReturnsEnabled = false;

        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Details_GrantCustomerRegistered_Challenge()
    {
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(true);

        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Details_GrantOtherStore_Challenge()
    {
        Grant(new Order { Id = "o1", StoreId = "s2" });

        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Details_NoGrantNoOwner_Challenge()
    {
        _httpContext.Request.Headers.Cookie = "";

        var result = await _controller.MerchandiseReturnDetails("r1");

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }

    [TestMethod]
    public async Task Grant_VisitorLoggedIn_NotServed()
    {
        //an account logged in on a browser a guest left their grant in acts on its own orders only
        _groups.Setup(x => x.IsRegistered(_visitor)).ReturnsAsync(true);

        Assert.IsInstanceOfType<ChallengeResult>(await _controller.MerchandiseReturnDetails("r1"));
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.MerchandiseReturn("o1"));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await _controller.MerchandiseReturnCancel("r1"));
    }

    [TestMethod]
    public async Task Details_Grant_ShownAsGuest()
    {
        var result = (ViewResult)await _controller.MerchandiseReturnDetails("r1");

        Assert.IsTrue(((MerchandiseReturnDetailsModel)result.Model).Guest);
    }

    [TestMethod]
    public async Task List_Grant_OrderReturnsOnly()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturns>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerMerchandiseReturnsModel());

        await _controller.CustomerMerchandiseReturns();

        _mediator.Verify(x => x.Send(It.Is<GetMerchandiseReturns>(c => c.OrderId == "o1" && c.Customer.Id == "g1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task List_Grant_ReturnButtonWhenAllowed()
    {
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturns>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CustomerMerchandiseReturnsModel());

        var allowed = (CustomerMerchandiseReturnsModel)((ViewResult)await _controller.CustomerMerchandiseReturns())
            .Model;
        _mediator.Setup(x => x.Send(It.IsAny<IsMerchandiseReturnAllowedQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var returned = (CustomerMerchandiseReturnsModel)((ViewResult)await _controller.CustomerMerchandiseReturns())
            .Model;

        Assert.AreEqual("o1", allowed.ReturnOrderId);
        Assert.IsNull(returned.ReturnOrderId);
        Assert.IsTrue(allowed.Guest);
    }

    [TestMethod]
    public async Task List_RegisteredWithGrant_OwnList()
    {
        _groups.Setup(x => x.IsRegistered(_visitor)).ReturnsAsync(true);
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturns>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CustomerMerchandiseReturnsModel());

        await _controller.CustomerMerchandiseReturns();

        _mediator.Verify(x => x.Send(It.Is<GetMerchandiseReturns>(c => c.OrderId == null && c.Customer == _visitor),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task List_NoGrantGuest_Challenge()
    {
        _httpContext.Request.Headers.Cookie = "";

        Assert.IsInstanceOfType<ChallengeResult>(await _controller.CustomerMerchandiseReturns());
    }
}
