using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authentication;
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
///     The guest grant of an order opens its digital products, licences, uploaded files and the files of the notes
///     shown to the customer, and nothing of another order; a note kept for the staff is given to no customer
/// </summary>
[TestClass]
public class DownloadControllerOrderGuestTests
{
    private static readonly Guid Item1 = Guid.NewGuid();
    private static readonly Guid Item2 = Guid.NewGuid();
    private static readonly Guid Upload1 = Guid.NewGuid();
    private static readonly Guid Upload2 = Guid.NewGuid();

    private DownloadController _controller;
    private DefaultHttpContext _httpContext;
    private OrderNote _note1;
    private ShipmentNote _shipmentNote1;
    private Mock<IShipmentService> _shipments;
    private Customer _visitor;

    [TestInitialize]
    public void Init()
    {
        var item1 = new OrderItem {
            Id = "i1", OrderItemGuid = Item1, ProductId = "p1", LicenseDownloadId = "lic",
            Attributes = { new CustomAttribute { Key = "a1", Value = Upload1.ToString() } }
        };
        var item2 = new OrderItem {
            Id = "i2", OrderItemGuid = Item2, ProductId = "p1", LicenseDownloadId = "lic",
            Attributes = { new CustomAttribute { Key = "a1", Value = Upload2.ToString() } }
        };
        var order1 = Paid(new Order { Id = "o1", CustomerId = "g1", StoreId = "s1", OrderItems = { item1 } });
        var order2 = Paid(new Order { Id = "o2", CustomerId = "g2", StoreId = "s1", OrderItems = { item2 } });
        _note1 = new OrderNote { Id = "n1", OrderId = "o1", DownloadId = "file", DisplayToCustomer = true };
        _shipmentNote1 = new ShipmentNote
            { Id = "sn1", ShipmentId = "sh1", DownloadId = "file", DisplayToCustomer = true };

        var orders = new Mock<IOrderService>();
        orders.Setup(x => x.GetOrderItemByGuid(Item1)).ReturnsAsync(item1);
        orders.Setup(x => x.GetOrderItemByGuid(Item2)).ReturnsAsync(item2);
        orders.Setup(x => x.GetOrderByOrderItemId("i1")).ReturnsAsync(order1);
        orders.Setup(x => x.GetOrderByOrderItemId("i2")).ReturnsAsync(order2);
        orders.Setup(x => x.GetOrderById("o1")).ReturnsAsync(order1);
        orders.Setup(x => x.GetOrderById("o2")).ReturnsAsync(order2);
        orders.Setup(x => x.GetOrderNote("n1")).ReturnsAsync(() => _note1);
        orders.Setup(x => x.GetOrderNote("n2"))
            .ReturnsAsync(new OrderNote { Id = "n2", OrderId = "o2", DownloadId = "file", DisplayToCustomer = true });
        _shipments = new Mock<IShipmentService>();
        _shipments.Setup(x => x.GetShipmentNote("sn1")).ReturnsAsync(() => _shipmentNote1);
        _shipments.Setup(x => x.GetShipmentNote("sn2")).ReturnsAsync(new ShipmentNote
            { Id = "sn2", ShipmentId = "sh2", DownloadId = "file", DisplayToCustomer = true });
        _shipments.Setup(x => x.GetShipmentById("sh1")).ReturnsAsync(new Shipment { Id = "sh1", OrderId = "o1" });
        _shipments.Setup(x => x.GetShipmentById("sh2")).ReturnsAsync(new Shipment { Id = "sh2", OrderId = "o2" });
        var products = new Mock<IProductService>();
        products.Setup(x => x.GetProductById("p1", It.IsAny<bool>())).ReturnsAsync(new Product {
            Id = "p1", IsDownload = true, DownloadId = "d1", UnlimitedDownloads = true,
            DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid
        });
        var downloads = new Mock<IDownloadService>();
        foreach (var id in new[] { "d1", "lic", "file" })
            downloads.Setup(x => x.GetDownloadById(id)).ReturnsAsync(File(id));
        downloads.Setup(x => x.GetDownloadByGuid(Upload1)).ReturnsAsync(File("u1", "g1"));
        downloads.Setup(x => x.GetDownloadByGuid(Upload2)).ReturnsAsync(File("u2", "g2"));
        var customers = new Mock<ICustomerService>();
        customers.Setup(x => x.GetCustomerById("g1")).ReturnsAsync(new Customer { Id = "g1" });
        customers.Setup(x => x.GetCustomerById("g2")).ReturnsAsync(new Customer { Id = "g2" });
        var groups = new Mock<IGroupService>();
        groups.Setup(x => x.IsRegistered(It.IsAny<Customer>())).ReturnsAsync(false);

        _visitor = new Customer { Id = "visitor" };
        var work = new Mock<IWorkContext>();
        work.Setup(x => x.CurrentCustomer).Returns(() => _visitor);
        var store = new Mock<IStoreContext>();
        store.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.WorkContext).Returns(work.Object);
        context.Setup(x => x.StoreContext).Returns(store.Object);

        var orderSettings = new OrderSettings { AllowGuestOrderAccess = true };
        _httpContext = new DefaultHttpContext {
            RequestServices = new ServiceCollection()
                .AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider())
                .AddSingleton(customers.Object)
                .AddSingleton(groups.Object)
                .AddSingleton(orderSettings)
                .AddSingleton<ICookieOptionsFactory>(new CookieOptionsFactory(new SecurityConfig()))
                .BuildServiceProvider()
        };
        var granting = new DefaultHttpContext { RequestServices = _httpContext.RequestServices };
        GuestOrderGrant.Grant(granting, order1);
        var header = granting.Response.Headers.SetCookie.ToString();
        _httpContext.Request.Headers.Cookie = header[..header.IndexOf(';')];

        _controller = new DownloadController(downloads.Object, products.Object, orders.Object,
            new Mock<IMerchandiseReturnService>().Object, context.Object,
            new Mock<IRequestTranslationService>().Object,
            new CustomerSettings { DownloadableProductsValidateUser = true }, groups.Object, orderSettings) {
            ControllerContext = new ControllerContext { HttpContext = _httpContext }
        };
    }

    private static Order Paid(Order order)
    {
        order.PaymentStatusId = PaymentStatus.Paid;
        order.PaidDateUtc = DateTime.UtcNow;
        return order;
    }

    private static Download File(string id, string customerId = null)
    {
        return new Download {
            Id = id, CustomerId = customerId, DownloadBinary = [1, 2], ContentType = "application/pdf",
            Filename = id, Extension = ".pdf"
        };
    }

    [TestMethod]
    public async Task Download_Grant_File()
    {
        Assert.IsInstanceOfType<FileContentResult>(await _controller.GetDownload(Item1));
    }

    [TestMethod]
    public async Task Download_GrantOtherOrder_Refused()
    {
        //the answer it gives any customer for another's order
        Assert.AreEqual("This is not your order", ((ContentResult)await _controller.GetDownload(Item2)).Content);
    }

    [TestMethod]
    public async Task License_Grant_File()
    {
        Assert.IsInstanceOfType<FileContentResult>(await _controller.GetLicense(Item1));
    }

    [TestMethod]
    public async Task License_GrantOtherOrder_Challenge()
    {
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.GetLicense(Item2));
    }

    [TestMethod]
    public async Task FileUpload_Grant_File()
    {
        //the order's page links the file the guest uploaded with the order
        Assert.IsInstanceOfType<FileContentResult>(await _controller.GetFileUpload(Upload1));
    }

    [TestMethod]
    public async Task FileUpload_GrantOtherOrder_Challenge()
    {
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.GetFileUpload(Upload2));
    }

    [TestMethod]
    public async Task OrderNoteFile_Grant_File()
    {
        Assert.IsInstanceOfType<FileContentResult>(await _controller.GetOrderNoteFile("n1"));
    }

    [TestMethod]
    public async Task OrderNoteFile_GrantStaffOnly_NotFound()
    {
        _note1.DisplayToCustomer = false;

        Assert.IsInstanceOfType<NotFoundResult>(await _controller.GetOrderNoteFile("n1"));
    }

    [TestMethod]
    public async Task OrderNoteFile_CustomerStaffOnly_NotFound()
    {
        //the order's own customer, logged in, is not given a note kept for the staff either
        _visitor = new Customer { Id = "g1" };
        _note1.DisplayToCustomer = false;

        Assert.IsInstanceOfType<NotFoundResult>(await _controller.GetOrderNoteFile("n1"));
    }

    [TestMethod]
    public async Task OrderNoteFile_GrantOtherOrder_Challenge()
    {
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.GetOrderNoteFile("n2"));
    }

    [TestMethod]
    public async Task ShipmentNoteFile_Grant_File()
    {
        Assert.IsInstanceOfType<FileContentResult>(
            await _controller.GetShipmentNoteFile("sn1", _shipments.Object));
    }

    [TestMethod]
    public async Task ShipmentNoteFile_CustomerStaffOnly_NotFound()
    {
        _visitor = new Customer { Id = "g1" };
        _shipmentNote1.DisplayToCustomer = false;

        Assert.IsInstanceOfType<NotFoundResult>(await _controller.GetShipmentNoteFile("sn1", _shipments.Object));
    }

    [TestMethod]
    public async Task ShipmentNoteFile_GrantOtherOrder_Challenge()
    {
        Assert.IsInstanceOfType<ChallengeResult>(await _controller.GetShipmentNoteFile("sn2", _shipments.Object));
    }
}
