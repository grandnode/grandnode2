using System.Buffers.Binary;
using System.IO.Compression;
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
    private Mock<IMediator> _mediatorMock;
    private Mock<IOrderService> _orderServiceMock;
    private PdfSettings _pdfSettings;
    private Mock<IPictureService> _pictureServiceMock;
    private Mock<IProductService> _productServiceMock;
    private PdfService _service;

    [TestInitialize]
    public void Init()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _productServiceMock = new Mock<IProductService>();
        _pictureServiceMock = new Mock<IPictureService>();
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

        _service = new PdfService(new Mock<IRepository<Download>>().Object, translationServiceMock.Object,
            new Mock<IStoreFilesContext>().Object, new Mock<IWebHostEnvironment>().Object,
            _productServiceMock.Object, new Mock<ICountryService>().Object,
            storeServiceMock.Object, _pictureServiceMock.Object, new Mock<IPaymentService>().Object,
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
    public async Task PrintOrdersToPdf_EmbedsTheLogoOnceForAllOrders()
    {
        //Arrange
        const string onePixelPng =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8DwHwAFBQIAX8jx0gAAAABJRU5ErkJggg==";
        _pdfSettings.LogoPictureId = "logo";
        var logo = new Picture { Id = "logo", MimeType = "image/png" };
        _pictureServiceMock.Setup(p => p.GetPictureById("logo")).ReturnsAsync(logo);
        _pictureServiceMock.Setup(p => p.LoadPictureBinary(logo)).ReturnsAsync(Convert.FromBase64String(onePixelPng));
        //Act
        using var single = new MemoryStream();
        await _service.PrintOrdersToPdf(single, [CreateOrder()]);
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, [CreateOrder(), CreateOrder(), CreateOrder()]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.AreEqual(3, pdf.NumberOfPages);
        foreach (var page in pdf.GetPages())
            Assert.HasCount(1, page.GetImages());
        //an RGBA PNG is written as an image plus its alpha mask; three pages must not add more of them
        Assert.AreEqual(ImageObjects(single), ImageObjects(stream));
    }

    private static int ImageObjects(MemoryStream pdf)
    {
        return Encoding.Latin1.GetString(pdf.ToArray()).Split("/Subtype /Image").Length - 1;
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
        using var stream = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        //Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.PrintPackagingSlipsToPdf(stream, [new Shipment()], "lang-1", cancellation.Token));
        Assert.AreEqual(0, stream.Length);
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_WithNoOrders_WritesOneEmptyPage()
    {
        //Act
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, []);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.AreEqual(1, pdf.NumberOfPages);
    }

    [TestMethod]
    public async Task PrintPackagingSlipsToPdf_WhenEveryOrderIsMissing_WritesOneEmptyPage()
    {
        //Act
        using var stream = new MemoryStream();
        await _service.PrintPackagingSlipsToPdf(stream, [new Shipment { OrderId = "deleted" }]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.AreEqual(1, pdf.NumberOfPages);
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_RowTallerThanAPage_IsSplitAcrossPagesAboveTheFooter()
    {
        //Arrange
        _pdfSettings.InvoiceFooterText = "Footer";
        var order = CreateOrder();
        order.OrderItems.First().AttributeDescription =
            string.Join("<br />", Enumerable.Range(1, 150).Select(i => $"Message line {i}"));
        //Act
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, [order]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.IsGreaterThan(1, pdf.NumberOfPages);
        var text = string.Join("\n", pdf.GetPages().Select(p => p.Text));
        StringAssert.Contains(text, "Message line 1");
        StringAssert.Contains(text, "Message line 150");
        StringAssert.Contains(text, "PDFInvoice.OrderTotal");
        foreach (var page in pdf.GetPages())
        {
            var words = page.GetWords().ToList();
            var footerTop = words.Single(w => w.Text == "Footer").BoundingBox.Top;
            Assert.IsTrue(words.Where(w => w.Text != "Footer").All(w => w.BoundingBox.Bottom > footerTop),
                $"page {page.Number} has text over the footer");
        }
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_FooterLongerThanThePage_IsCutAndKeepsOnePage()
    {
        //Arrange
        _pdfSettings.InvoiceFooterText =
            string.Join("<br />", Enumerable.Range(1, 200).Select(i => $"Footer line {i}"));
        //Act
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, [CreateOrder()]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.AreEqual(1, pdf.NumberOfPages);
        StringAssert.Contains(pdf.GetPage(1).Text, "PDFInvoice.OrderTotal");
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_HeaderTextWrapsBesideTheLogo()
    {
        //Arrange
        SetLogo(CreatePng(150, 60));
        _pdfSettings.InvoiceHeaderText = string.Join(" ", Enumerable.Repeat("Header", 80));
        //Act
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, [CreateOrder()]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        var page = pdf.GetPage(1);
        var logo = page.GetImages().Single().BoundingBox;
        var overLogo = page.Letters.Where(l => l.GlyphRectangle.Right > logo.Left &&
                                               l.GlyphRectangle.Top > logo.Bottom);
        Assert.IsFalse(overLogo.Any(), "text is drawn over the logo");
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_LogoPdfPigCannotRead_IsConvertedInsteadOfFailingTheInvoice()
    {
        //Arrange
        var broken = CreatePng(20, 20);
        //corrupts the compressed image data while the PNG header stays readable
        broken[40] ^= 0xFF;
        broken[41] ^= 0xFF;
        SetLogo(broken);
        _pictureServiceMock.Setup(p => p.ConvertPicture(broken, 100, "Png")).Returns(CreatePng(20, 20));
        //Act
        using var stream = new MemoryStream();
        await _service.PrintOrdersToPdf(stream, [CreateOrder()]);
        //Assert
        using var pdf = PdfDocument.Open(stream.ToArray());
        Assert.HasCount(1, pdf.GetPage(1).GetImages());
    }

    [TestMethod]
    public async Task PrintOrdersToPdf_FooterTableCells_KeepASpaceAndHideStyles()
    {
        //Arrange
        _pdfSettings.InvoiceFooterText =
            "<style>.x{color:red}</style><table><tr><td>Bank: PKO</td><td>IBAN: PL61</td></tr></table>";
        //Act
        var text = await PrintOrders([CreateOrder()]);
        //Assert
        StringAssert.Contains(text, "PKO IBAN");
        Assert.DoesNotContain("color", text);
    }

    private void SetLogo(byte[] png)
    {
        _pdfSettings.LogoPictureId = "logo";
        var logo = new Picture { Id = "logo", MimeType = "image/png" };
        _pictureServiceMock.Setup(p => p.GetPictureById("logo")).ReturnsAsync(logo);
        _pictureServiceMock.Setup(p => p.LoadPictureBinary(logo)).ReturnsAsync(png);
    }

    /// <summary>
    ///     A gray 8-bit RGB PNG of the given size.
    /// </summary>
    private static byte[] CreatePng(int width, int height)
    {
        using var raw = new MemoryStream();
        using (var zlib = new ZLibStream(raw, CompressionLevel.Optimal, true))
        {
            var row = new byte[1 + width * 3];
            Array.Fill(row, (byte)128, 1, width * 3);
            for (var y = 0; y < height; y++) zlib.Write(row);
        }

        using var png = new MemoryStream();
        png.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        WriteChunk(png, "IHDR", header);
        WriteChunk(png, "IDAT", raw.ToArray());
        WriteChunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void WriteChunk(Stream png, string type, byte[] data)
    {
        var length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        png.Write(length);
        var typeAndData = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        png.Write(typeAndData);
        var crc = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeAndData));
        png.Write(crc);
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        return ~crc;
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
