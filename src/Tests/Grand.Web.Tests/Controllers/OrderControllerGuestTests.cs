using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
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
///     A guest asks for the link to an order and opens it; a visitor holding the grant of order o1 sees o1 as its
///     customer, and nothing else
/// </summary>
[TestClass]
public class OrderControllerGuestTests
{
    private readonly Customer _guest = new() { Id = "g1", Email = "guest@example.com" };
    private readonly Customer _visitor = new() { Id = "visitor" };
    private OrderController _controller;
    private Mock<IGroupService> _groups;
    private DefaultHttpContext _httpContext;
    private Mock<IMediator> _mediator;
    private readonly Order _order1 = new() { Id = "o1", CustomerId = "g1", StoreId = "s1" };
    private readonly Order _order2 = new() { Id = "o2", CustomerId = "g2", StoreId = "s1" };

    private readonly OrderSettings _orderSettings =
        new() { MerchandiseReturnsEnabled = true, AllowGuestOrderAccess = true };

    private Mock<IOrderService> _orders;
    private Mock<IPaymentService> _payments;
    private Mock<IPaymentTransactionService> _paymentTransactions;
    private IDataProtectionProvider _provider;
    private Mock<IWorkContext> _work;

    [TestInitialize]
    public void Init()
    {
        _work = new Mock<IWorkContext>();
        _work.Setup(x => x.CurrentCustomer).Returns(_visitor);
        _work.Setup(x => x.WorkingLanguage).Returns(new Language { Id = "l1" });
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(_work.Object);
        context.Setup(x => x.StoreContext).Returns(storeContext.Object);

        _orders = new Mock<IOrderService>();
        _orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(_order1);
        _orders.Setup(x => x.GetOrderById("o2")).ReturnsAsync(_order2);
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(_guest);
        customers.Setup(x => x.GetCustomerById("g2")).ReturnsAsync(new Customer { Id = "g2" });

        _groups = new Mock<IGroupService>();
        _groups.Setup(x => x.IsOwner(It.IsAny<Customer>())).ReturnsAsync(true);
        _groups.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);
        _mediator = new Mock<IMediator>();
        _payments = new Mock<IPaymentService>();
        _paymentTransactions = new Mock<IPaymentTransactionService>();
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

        _controller = new OrderController(_orders.Object, context.Object, _payments.Object,
            _paymentTransactions.Object, translation.Object, _groups.Object, _mediator.Object, _orderSettings) {
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

    private ICustomerService Customers => _httpContext.RequestServices.GetRequiredService<ICustomerService>();

    [TestMethod]
    public async Task GuestPage_SettingOff_NotFound()
    {
        _orderSettings.AllowGuestOrderAccess = false;

        Assert.IsInstanceOfType<NotFoundResult>(_controller.GuestOrder(new CaptchaSettings()));
        //a POST answered with an empty 404 is re-executed as a POST of the not-found page
        Assert.IsInstanceOfType<NotFoundObjectResult>(
            await _controller.GuestOrder(new GuestOrderModel(), new CaptchaSettings()));
        Assert.IsInstanceOfType<NotFoundResult>(
            await _controller.GuestOrderAccess("token", _provider, Customers));
    }

    [TestMethod]
    public void ReturnsDisabled_GuestOrderPageStillServed()
    {
        _orderSettings.MerchandiseReturnsEnabled = false;

        Assert.IsInstanceOfType<ViewResult>(_controller.GuestOrder(new CaptchaSettings()));
    }

    [TestMethod]
    public async Task GuestPost_AnyOutcome_SameView()
    {
        //Arrange - an order that matched, then one that did not
        _mediator.SetupSequence(x =>
                x.Send(It.IsAny<SendGuestOrderLinkCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(false);

        //Act
        var sent = (ViewResult)await _controller.GuestOrder(
            new GuestOrderModel { OrderCode = "K7Q2M9XA", Email = "guest@example.com" }, new CaptchaSettings());
        var notSent = (ViewResult)await _controller.GuestOrder(
            new GuestOrderModel { OrderCode = "K7Q2M9XB", Email = "guest@example.com" }, new CaptchaSettings());

        //Assert
        Assert.AreEqual(sent.ViewName, notSent.ViewName);
        Assert.AreEqual(((GuestOrderModel)sent.Model).Result, ((GuestOrderModel)notSent.Model).Result);
        Assert.IsNotNull(((GuestOrderModel)sent.Model).Result);
        Assert.IsNull(sent.StatusCode);
        Assert.IsNull(notSent.StatusCode);
    }

    [TestMethod]
    public async Task GuestPost_InvalidModel_NoCommand()
    {
        _controller.ModelState.AddModelError("Email", "Enter the e-mail address.");

        var result = await _controller.GuestOrder(new GuestOrderModel(), new CaptchaSettings());

        Assert.IsInstanceOfType<ViewResult>(result);
        _mediator.Verify(x => x.Send(It.IsAny<SendGuestOrderLinkCommand>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task Access_GoodToken_SetsCookie()
    {
        var token = GuestOrderGrant.LinkToken(_provider, _order1);

        var result = await _controller.GuestOrderAccess(token, _provider, Customers);

        Assert.IsInstanceOfType<RedirectToRouteResult>(result);
        StringAssert.Contains(_httpContext.Response.Headers.SetCookie.ToString(),
            GuestOrderGrant.CookieName(new CookieOptionsFactory(new SecurityConfig())));
    }

    [TestMethod]
    public async Task Access_BadToken_RedirectsToGuestPageWithoutCookie()
    {
        var result = await _controller.GuestOrderAccess("not-a-token", _provider, Customers);

        Assert.AreEqual("GuestOrder", ((RedirectToRouteResult)result).RouteName);
        Assert.AreEqual("", _httpContext.Response.Headers.SetCookie.ToString());
    }

    [TestMethod]
    public async Task Access_VisitorLoggedIn_RedirectsToOwnOrdersWithoutCookie()
    {
        _groups.Setup(x => x.IsRegistered(_visitor)).ReturnsAsync(true);
        var token = GuestOrderGrant.LinkToken(_provider, _order1);

        var result = await _controller.GuestOrderAccess(token, _provider, Customers);

        Assert.AreEqual("CustomerOrders", ((RedirectToRouteResult)result).RouteName);
        Assert.AreEqual("", _httpContext.Response.Headers.SetCookie.ToString());
    }

    [TestMethod]
    public async Task Access_RegisteredCustomer_RedirectsToGuestPageWithoutCookie()
    {
        _groups.Setup(x => x.IsRegistered(_guest)).ReturnsAsync(true);
        var token = GuestOrderGrant.LinkToken(_provider, _order1);

        var result = await _controller.GuestOrderAccess(token, _provider, Customers);

        Assert.AreEqual("GuestOrder", ((RedirectToRouteResult)result).RouteName);
        Assert.AreEqual("", _httpContext.Response.Headers.SetCookie.ToString());
    }
}
