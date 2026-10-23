using Grand.Business.Common.Services.Pdf;
using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UglyToad.PdfPig;

namespace Grand.Business.Common.Tests.Services.Pdf;

[TestClass]
public class PdfServiceTests
{
    private Mock<ILanguageService> _languageServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private PdfSettings _pdfSettings;
    private Mock<IProductService> _productServiceMock;
    private PdfService _service;

    [TestInitialize]
    public void Init()
    {
        _languageServiceMock = new Mock<ILanguageService>();
        _orderServiceMock = new Mock<IOrderService>();
        _productServiceMock = new Mock<IProductService>();
        _pdfSettings = new PdfSettings();
        _mediatorMock = new Mock<IMediator>();

        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock
            .Setup(t => t.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>()))
            .Returns((string key, string _, string _, bool _) => key + " {0}");

        var storeServiceMock = new Mock<IStoreService>();
        storeServiceMock.Setup(s => s.GetStoreById(It.IsAny<string>()))
            .ReturnsAsync(new Store { CompanyName = "Sklep Łódź sp. z o.o." });

        var giftVoucherServiceMock = new Mock<IGiftVoucherService>();
        giftVoucherServiceMock.Setup(g => g.GetAllGiftVoucherUsageHistory(It.IsAny<string>()))
            .ReturnsAsync(new List<GiftVoucherUsageHistory>());

        _productServiceMock.Setup(p => p.GetProductByIdIncludeArch(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Product { Id = id, Name = "Product " + id, Sku = "SKU-" + id });

        _service = new PdfService(new Mock<IRepository<Download>>().Object, _languageServiceMock.Object,
            translationServiceMock.Object, new Mock<IStoreFilesContext>().Object,
            new Mock<IWebHostEnvironment>().Object, _productServiceMock.Object, new Mock<ICountryService>().Object,
            storeServiceMock.Object, new Mock<IPictureService>().Object, new Mock<IPaymentService>().Object,
            giftVoucherServiceMock.Object, _orderServiceMock.Object, _pdfSettings, _mediatorMock.Object,
            new Mock<ILogger<PdfService>>().Object);
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_WritesInvoiceWithPolishCharactersAndTotals()
    {
        //Arrange
        var order = CreateOrder();
        //Act
        var text = await PrintOrders([order]);
        //Assert
        StringAssert.Contains(text, "Sklep Łódź sp. z o.o.");
        StringAssert.Contains(text, "Zażółć Gęślą");
        StringAssert.Contains(text, "Product p1");
        StringAssert.Contains(text, "Product p2");
        StringAssert.Contains(text, "PDFInvoice.OrderTotal");
        StringAssert.Contains(text, "PLN");
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_DropsMarkupFromAttributeDescription()
    {
        //Arrange
        var order = CreateOrder();
        order.OrderItems.First().AttributeDescription = "Color: <b>Red</b><br />Size: M &amp; L";
        //Act
        var text = await PrintOrders([order]);
        //Assert
        StringAssert.Contains(text, "Color: Red");
        StringAssert.Contains(text, "Size: M & L");
        Assert.DoesNotContain("<br", text);
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_PrintsLinesAddedByPaymentLinesHandlers()
    {
        //Arrange
        _mediatorMock
            .Setup(m => m.Publish(It.IsAny<OrderPdfPaymentLinesEvent>(), It.IsAny<CancellationToken>()))
            .Callback((OrderPdfPaymentLinesEvent e, CancellationToken _) => e.Lines.Add("Transfer title: <b>1001</b>"))
            .Returns(Task.CompletedTask);
        //Act
        var text = await PrintOrders([CreateOrder()]);
        //Assert
        StringAssert.Contains(text, "Transfer title: 1001");
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_ForVendor_PrintsOnlyVendorLinesWithoutTotals()
    {
        //Arrange
        var order = CreateOrder();
        //Act
        var text = await PrintOrders([order], "vendor-1");
        //Assert
        StringAssert.Contains(text, "Product p1");
        Assert.DoesNotContain("Product p2", text);
        Assert.DoesNotContain("PDFInvoice.OrderTotal", text);
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_ReplacesCharactersMissingFromTheFont()
    {
        //Arrange
        var order = CreateOrder();
        order.BillingAddress.FirstName = "中文";
        //Act
        var text = await PrintOrders([order]);
        //Assert
        StringAssert.Contains(text, "?? Gęślą");
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_StartsEveryOrderOnANewPageAndRepeatsTheFooter()
    {
        //Arrange
        _pdfSettings.InvoiceFooterText = "<p>Footer &amp; bank account</p>";
        var many = CreateOrder();
        for (var i = 3; i < 60; i++)
            many.OrderItems.Add(new OrderItem { Id = "i" + i, ProductId = "p" + i, Quantity = 1 });
        //Act
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, [many, CreateOrder()]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.IsGreaterThan(2, pdf.NumberOfPages);
        foreach (var page in pdf.GetPages())
            StringAssert.Contains(page.Text, "Footer & bank account");
        StringAssert.Contains(pdf.GetPage(pdf.NumberOfPages).Text, "Product p1");
    }

    [TestMethod]
    public async Task PrintPackagingSlipsToPdf_ForVendorShipment_PrintsOnlyVendorLines()
    {
        //Arrange
        var order = CreateOrder();
        _orderServiceMock.Setup(o => o.GetOrderById(order.Id)).ReturnsAsync(order);
        _languageServiceMock.Setup(l => l.GetLanguageById("lang-1")).ReturnsAsync(new Language { Id = "lang-1" });
        var shipment = new Shipment { OrderId = order.Id, ShipmentNumber = 7, VendorId = "vendor-1" };
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "i1", ProductId = "p1", Quantity = 2 });
        shipment.ShipmentItems.Add(new ShipmentItem { OrderItemId = "i2", ProductId = "p2", Quantity = 1 });
        //Act
        using var stream = new MemoryStream();
        await _service.PrintPackagingSlipsToPdf(stream, [shipment], "lang-1");
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        var text = pdf.GetPage(1).Text;
        StringAssert.Contains(text, "SKU-p1");
        Assert.DoesNotContain("Product p2", text);
        StringAssert.Contains(text, "Zażółć Gęślą");
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_WhenRequestCancelled_DoesNotWriteThePdf()
    {
        //Arrange
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.PrintOrdersToPdf(stream, [CreateOrder()], cancellationToken: cancellation.Token));
        Assert.AreEqual(0, stream.Length);
    }

    [TestMethod]
    public async Task PrintPackagingSlipsToPdf_WhenRequestCancelled_DoesNotWriteThePdf()
    {
        //Arrange
        _languageServiceMock.Setup(l => l.GetLanguageById("lang-1")).ReturnsAsync(new Language { Id = "lang-1" });
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.PrintPackagingSlipsToPdf(stream, [new Shipment()], "lang-1", cancellation.Token));
        Assert.AreEqual(0, stream.Length);
    }

    private async Task<string> PrintOrders(IList<Order> orders, string vendorId = "")
    {
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, orders, vendorId: vendorId);
        using var pdf = PdfDocument.Open(stream.ToArray());
        return string.Join("\n", pdf.GetPages().Select(p => p.Text));
    }

    private static Order CreateOrder()
    {
        var address = new Address {
            FirstName = "Zażółć", LastName = "Gęślą", City = "Łódź", ZipPostalCode = "90-001",
            Address1 = "ul. Piotrkowska 1", PhoneNumber = "+48 600 000 000"
        };
        var order = new Order {
            Id = "order-1",
            OrderNumber = 1001,
            CustomerCurrencyCode = "PLN",
            BillingAddress = address,
            ShippingAddress = address,
            ShippingMethod = "Kurier",
            OrderSubtotalExclTax = 150,
            OrderShippingExclTax = 15,
            OrderTax = 37.95,
            OrderTotal = 202.95
        };
        order.OrderItems.Add(new OrderItem {
            Id = "i1", ProductId = "p1", VendorId = "vendor-1", Quantity = 2, UnitPriceExclTax = 50,
            PriceExclTax = 100
        });
        order.OrderItems.Add(new OrderItem {
            Id = "i2", ProductId = "p2", VendorId = "vendor-2", Quantity = 1, UnitPriceExclTax = 50,
            PriceExclTax = 50
        });
        return order;
    }
}
