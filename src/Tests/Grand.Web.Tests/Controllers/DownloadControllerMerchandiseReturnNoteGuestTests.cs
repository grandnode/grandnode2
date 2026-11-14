using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Common.Localization;
using Grand.Web.Controllers;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     The guest grant of an order opens the files of its returns' notes shown to the customer, and nothing else
/// </summary>
[TestClass]
public class DownloadControllerMerchandiseReturnNoteGuestTests
{
    private DownloadController _controller;
    private Mock<IGroupService> _groupService;
    private DefaultHttpContext _httpContext;
    private MerchandiseReturnNote _note;
    private Order _order;

    private readonly OrderSettings _orderSettings =
        new() { MerchandiseReturnsEnabled = true, AllowGuestOrderAccess = true };

    [TestInitialize]
    public void Init()
    {
        _note = new MerchandiseReturnNote
            { Id = "n1", MerchandiseReturnId = "r1", DownloadId = "d1", DisplayToCustomer = true };
        _order = new Order { Id = "o1", CustomerId = "g1", StoreId = "s1" };

        var returns = new Mock<IMerchandiseReturnService>();
        returns.Setup(x => x.GetMerchandiseReturnNote("n1")).ReturnsAsync(() => _note);
        returns.Setup(x => x.GetMerchandiseReturnById("r1"))
            .ReturnsAsync(new MerchandiseReturn { Id = "r1", OrderId = "o1", CustomerId = "g1" });
        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(() => _order);
        var downloads = new Mock<IDownloadService>();
        downloads.Setup(x => x.GetDownloadById("d1")).ReturnsAsync(new Download {
            Id = "d1", DownloadBinary = [1, 2], ContentType = "image/png", Filename = "photo", Extension = ".png"
        });
        var guest = new Customer { Id = "g1" };
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(guest);

        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "visitor" });
        var store = new Mock<IStoreContext>();
        store.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        context.Setup(x => x.StoreContext).Returns(store.Object);

        _groupService = new Mock<IGroupService>();
        _groupService.Setup(x => x.IsOwner(It.IsAny<Customer>())).ReturnsAsync(true);
        _groupService.Setup(x => x.IsRegistered(guest)).ReturnsAsync(false);

        _httpContext = new DefaultHttpContext {
            RequestServices = new ServiceCollection()
                .AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider())
                .AddSingleton(customers.Object)
                .AddSingleton(_orderSettings)
                .AddSingleton<ICookieOptionsFactory>(new CookieOptionsFactory(new SecurityConfig()))
                .BuildServiceProvider()
        };
        Grant(_order);

        _controller = new DownloadController(downloads.Object, new Mock<IProductService>().Object, orders.Object,
            returns.Object, context.Object, new Mock<IRequestTranslationService>().Object, new CustomerSettings()) {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private void Grant(Order order)
    {
        var granting = new DefaultHttpContext { RequestServices = _httpContext.RequestServices };
        GuestOrderGrant.Grant(granting, order);
        var header = granting.Response.Headers.SetCookie.ToString();
        _httpContext.Request.Headers.Cookie = header[..header.IndexOf(';')];
    }

    private Task<IActionResult> Download()
    {
        return _controller.GetMerchandiseReturnNoteFile("n1", _groupService.Object);
    }

    [TestMethod]
    public async Task NoteFile_Grant_File()
    {
        Assert.IsInstanceOfType<FileContentResult>(await Download());
    }

    [TestMethod]
    public async Task NoteFile_GrantOtherOrder_Challenge()
    {
        Grant(new Order { Id = "o2", StoreId = "s1" });

        Assert.IsInstanceOfType<ChallengeResult>(await Download());
    }

    [TestMethod]
    public async Task NoteFile_GrantReturnsDisabled_Challenge()
    {
        _orderSettings.MerchandiseReturnsEnabled = false;

        Assert.IsInstanceOfType<ChallengeResult>(await Download());
    }

    [TestMethod]
    public async Task NoteFile_GrantStaffOnlyNote_NotFound()
    {
        _note.DisplayToCustomer = false;

        Assert.IsInstanceOfType<NotFoundResult>(await Download());
    }
}
