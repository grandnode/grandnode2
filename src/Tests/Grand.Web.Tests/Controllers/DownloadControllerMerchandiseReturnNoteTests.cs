using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authentication;
using Grand.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     The file of a merchandise return note is given only to whoever may see the return, in the store of its order,
///     and only for a note shown to the customer
/// </summary>
[TestClass]
public class DownloadControllerMerchandiseReturnNoteTests
{
    private readonly Customer _buyer = new() { Id = "buyer" };
    private DownloadController _controller;
    private Mock<IGroupService> _groupService;
    private MerchandiseReturnNote _note;
    private Order _order;
    private MerchandiseReturn _return;

    [TestInitialize]
    public void Init()
    {
        _note = new MerchandiseReturnNote
            { Id = "n1", MerchandiseReturnId = "r1", DownloadId = "d1", DisplayToCustomer = true };
        _return = new MerchandiseReturn { Id = "r1", OrderId = "o1", CustomerId = "buyer" };
        _order = new Order { Id = "o1", CustomerId = "buyer", StoreId = "s1" };

        var returns = new Mock<IMerchandiseReturnService>();
        returns.Setup(x => x.GetMerchandiseReturnNote("n1")).ReturnsAsync(() => _note);
        returns.Setup(x => x.GetMerchandiseReturnById("r1")).ReturnsAsync(() => _return);
        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(() => _order);
        var downloads = new Mock<IDownloadService>();
        downloads.Setup(x => x.GetDownloadById("d1")).ReturnsAsync(new Download {
            Id = "d1", DownloadBinary = [1, 2], ContentType = "image/png", Filename = "photo", Extension = ".png"
        });

        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(_buyer);
        var store = new Mock<IStoreContext>();
        store.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        context.Setup(x => x.StoreContext).Returns(store.Object);

        _groupService = new Mock<IGroupService>();
        _groupService.Setup(x => x.IsOwner(_buyer)).ReturnsAsync(true);

        _controller = new DownloadController(downloads.Object, new Mock<IProductService>().Object, orders.Object,
            returns.Object, context.Object, new Mock<IRequestTranslationService>().Object, new CustomerSettings(),
            _groupService.Object, new OrderSettings()) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    RequestServices = new ServiceCollection()
                        .AddSingleton<ICookieOptionsFactory>(new CookieOptionsFactory(new SecurityConfig()))
                        .BuildServiceProvider()
                }
            }
        };
    }

    private Task<IActionResult> Download()
    {
        return _controller.GetMerchandiseReturnNoteFile("n1");
    }

    [TestMethod]
    public async Task NoteFile_Customer_File()
    {
        var result = await Download();

        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    [TestMethod]
    public async Task NoteFile_NotDisplayedToCustomer_NotFound()
    {
        _note.DisplayToCustomer = false;

        var result = await Download();

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task NoteFile_OtherStore_NotFound()
    {
        _order.StoreId = "s2";

        var result = await Download();

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task NoteFile_OwnerOfSubAccount_File()
    {
        //the return of a sub-account, read by the account that owns it
        _return.CustomerId = "sub";
        _return.OwnerId = "buyer";
        _order.CustomerId = "sub";
        _order.OwnerId = "buyer";

        var result = await Download();

        Assert.IsInstanceOfType<FileContentResult>(result);
    }

    [TestMethod]
    public async Task NoteFile_OtherCustomer_Challenge()
    {
        _return.CustomerId = "other";
        _order.CustomerId = "other";

        var result = await Download();

        Assert.IsInstanceOfType<ChallengeResult>(result);
    }
}
