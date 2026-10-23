using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Extensions;
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
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;

namespace Grand.Business.Common.Services.Pdf;

/// <summary>
///     Generates order invoices and packaging slips as PDF documents
/// </summary>
public class PdfService : IPdfService
{
    private const double LogoMaxWidth = 150;
    private const double LogoMaxHeight = 60;

    private readonly ICountryService _countryService;
    private readonly IRepository<Download> _downloadRepository;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly ILanguageService _languageService;
    private readonly ILogger<PdfService> _logger;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly PdfSettings _pdfSettings;
    private readonly IPictureService _pictureService;
    private readonly IProductService _productService;
    private readonly IStoreFilesContext _storeFilesContext;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PdfService(
        IRepository<Download> downloadRepository,
        ILanguageService languageService,
        ITranslationService translationService,
        IStoreFilesContext storeFilesContext,
        IWebHostEnvironment webHostEnvironment,
        IProductService productService,
        ICountryService countryService,
        IStoreService storeService,
        IPictureService pictureService,
        IPaymentService paymentService,
        IGiftVoucherService giftVoucherService,
        IOrderService orderService,
        PdfSettings pdfSettings,
        IMediator mediator,
        ILogger<PdfService> logger)
    {
        _downloadRepository = downloadRepository;
        _languageService = languageService;
        _translationService = translationService;
        _storeFilesContext = storeFilesContext;
        _webHostEnvironment = webHostEnvironment;
        _productService = productService;
        _countryService = countryService;
        _storeService = storeService;
        _pictureService = pictureService;
        _paymentService = paymentService;
        _giftVoucherService = giftVoucherService;
        _orderService = orderService;
        _pdfSettings = pdfSettings;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PrintOrdersToPdf(Stream stream, IList<Order> orders, string languageId = "",
        string vendorId = "", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(orders);

        using var pdf = new PdfComposer();
        var logo = await LoadLogo();
        foreach (var order in orders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ComposeOrder(pdf, order, vendorId, logo);
        }

        //nothing is written until the whole document is laid out, so a cancelled request leaves the stream empty
        cancellationToken.ThrowIfCancellationRequested();
        await stream.WriteAsync(pdf.Build(), cancellationToken);
    }

    public async Task<string> PrintOrderToPdf(Order order, string languageId, string vendorId = "")
    {
        ArgumentNullException.ThrowIfNull(order);

        var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}.pdf";

        var dir = Path.Combine(_webHostEnvironment.WebRootPath, "assets/files/exportimport");
        ArgumentNullException.ThrowIfNull(dir);

        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await PrintOrdersToPdf(fileStream, [order], languageId, vendorId);
        return filePath;
    }

    public async Task PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, string languageId = "",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(shipments);

        var lang = await _languageService.GetLanguageById(languageId);
        if (lang == null)
            throw new ArgumentException($"Cannot load language. ID={languageId}");

        using var pdf = new PdfComposer();
        foreach (var shipment in shipments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ComposePackagingSlip(pdf, shipment);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await stream.WriteAsync(pdf.Build(), cancellationToken);
    }

    public async Task<string> SaveOrderToBinary(Order order, string languageId, string vendorId = "")
    {
        ArgumentNullException.ThrowIfNull(order);

        var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}";
        using var ms = new MemoryStream();
        await PrintOrdersToPdf(ms, [order], languageId, vendorId);
        var download = new Download {
            Filename = fileName,
            Extension = ".pdf",
            ContentType = "application/pdf"
        };

        download.DownloadObjectId = await _storeFilesContext.BucketUploadFromBytes(download.Filename, ms.ToArray());
        await _downloadRepository.InsertAsync(download);

        return download.Id;
    }

    private async Task ComposeOrder(PdfComposer pdf, Order order, string vendorId, byte[] logo)
    {
        var lang = order.CustomerLanguageId;
        var limitByVendor = !string.IsNullOrEmpty(vendorId);
        var priceIncludesTax = order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
        var store = await _storeService.GetStoreById(order.StoreId);

        pdf.NewPage(PdfHtmlText.ToPlainText(_pdfSettings.InvoiceFooterText));
        if (logo != null) pdf.Image(logo, LogoMaxWidth, LogoMaxHeight, PdfAlign.Right);
        pdf.Text(PdfHtmlText.ToPlainText(_pdfSettings.InvoiceHeaderText), 9, gray: 90);
        pdf.Text(store?.CompanyName, 14, true);
        pdf.Text($"{Format("PDFInvoice.Order#", lang, order.OrderNumber)} ({order.CreatedOnUtc})", 11, true);
        if (logo != null) pdf.Space(Math.Max(0, LogoMaxHeight - 40));
        pdf.Space(8);

        var billing = PdfCell.Empty;
        if (order.BillingAddress != null)
        {
            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
            var paymentLines = new OrderPdfPaymentLinesEvent(order, lang);
            await _mediator.Publish(paymentLines);
            billing = await AddressCell(order.BillingAddress, "PDFInvoice", "PDFInvoice.BillingInformation", lang,
                Format("PDFInvoice.PaymentMethod", lang, paymentMethod.GetPaymentMethodName(order)),
                paymentLines.Lines.Select(PdfHtmlText.ToPlainText));
        }

        var shipping = order.ShippingAddress == null
            ? PdfCell.Empty
            : await AddressCell(order.ShippingAddress, "PDFInvoice", "PDFInvoice.ShippingInformation", lang,
                Format("PDFInvoice.ShippingMethod", lang, order.ShippingMethod));
        pdf.HorizontalRule(220);
        pdf.Table([new PdfColumn(1), new PdfColumn(1)], null, [[billing, shipping]]);

        pdf.Space(14);
        pdf.Text(_translationService.GetResource("PDFInvoice.Product(s)", lang));
        pdf.Space(4);

        var rows = new List<IReadOnlyList<PdfCell>>();
        foreach (var item in order.OrderItems.Where(x => !limitByVendor || x.VendorId == vendorId))
        {
            var product = await _productService.GetProductByIdIncludeArch(item.ProductId);
            var name = new List<PdfLine> { new(product?.GetTranslation(x => x.Name, lang) ?? item.ProductId) };
            var attributes = PdfHtmlText.ToPlainText(item.AttributeDescription);
            if (attributes.Length > 0) name.Add(new PdfLine(attributes, Small: true));

            rows.Add([
                new PdfCell(name),
                PdfCell.Of((priceIncludesTax ? item.UnitPriceInclTax : item.UnitPriceExclTax).ToString("N2")),
                PdfCell.Of(item.Quantity.ToString()),
                PdfCell.Of((priceIncludesTax ? item.PriceInclTax : item.PriceExclTax).ToString("N2"), true)
            ]);
        }

        pdf.Table(
            [
                new PdfColumn(6), new PdfColumn(2, PdfAlign.Right), new PdfColumn(1.2, PdfAlign.Right),
                new PdfColumn(2, PdfAlign.Right)
            ],
            [
                _translationService.GetResource("PDFInvoice.ProductName", lang),
                _translationService.GetResource("PDFInvoice.ProductPrice", lang),
                _translationService.GetResource("PDFInvoice.ProductQuantity", lang),
                _translationService.GetResource("PDFInvoice.ProductTotal", lang)
            ],
            rows);

        //totals cover the whole order, so a vendor's copy leaves them out
        if (limitByVendor) return;

        pdf.Space(10);
        pdf.Text(PdfHtmlText.ToPlainText(order.CheckoutAttributeDescription), 9);
        pdf.Table(
            [new PdfColumn(5), new PdfColumn(3, PdfAlign.Right), new PdfColumn(2, PdfAlign.Right)],
            null,
            await Totals(order, priceIncludesTax, lang),
            rowLines: false);
    }

    private async Task<List<IReadOnlyList<PdfCell>>> Totals(Order order, bool priceIncludesTax, string lang)
    {
        var totals = new List<IReadOnlyList<PdfCell>>();

        void Add(string label, double amount, bool bold = false)
        {
            totals.Add([PdfCell.Empty, PdfCell.Of(label, bold), PdfCell.Of(Money(order, amount), bold)]);
        }

        Add(_translationService.GetResource("PDFInvoice.Sub-Total", lang),
            priceIncludesTax ? order.OrderSubtotalInclTax : order.OrderSubtotalExclTax);
        if (order.OrderSubTotalDiscountInclTax > 0 || order.OrderSubTotalDiscountExclTax > 0)
            Add(_translationService.GetResource("PDFInvoice.Discount", lang),
                priceIncludesTax ? order.OrderSubTotalDiscountInclTax : order.OrderSubTotalDiscountExclTax);
        if (order.PaymentMethodAdditionalFeeInclTax > 0 || order.PaymentMethodAdditionalFeeExclTax > 0)
            Add(_translationService.GetResource("PDFInvoice.PaymentmethodAdditionalFee", lang),
                priceIncludesTax ? order.PaymentMethodAdditionalFeeInclTax : order.PaymentMethodAdditionalFeeExclTax);
        Add(_translationService.GetResource("PDFInvoice.Shipping", lang),
            priceIncludesTax ? order.OrderShippingInclTax : order.OrderShippingExclTax);
        if (order.OrderDiscount > 0)
            Add(_translationService.GetResource("PDFInvoice.Discount", lang), order.OrderDiscount);

        foreach (var usage in await _giftVoucherService.GetAllGiftVoucherUsageHistory(order.Id))
        {
            if (usage.UsedValue <= 0) continue;
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(usage.GiftVoucherId);
            Add(Format("PDFInvoice.GiftVoucherInfo", lang, giftVoucher?.Code), usage.UsedValue);
        }

        if (order.OrderTax > 0)
            Add(_translationService.GetResource("PDFInvoice.Tax", lang), order.OrderTax);
        Add(_translationService.GetResource("PDFInvoice.OrderTotal", lang), order.OrderTotal, true);

        return totals;
    }

    private async Task ComposePackagingSlip(PdfComposer pdf, Shipment shipment)
    {
        var order = await _orderService.GetOrderById(shipment.OrderId);
        if (order == null) return;

        var lang = order.CustomerLanguageId;
        var store = await _storeService.GetStoreById(order.StoreId);

        pdf.NewPage();
        pdf.Text(Format("PDFPackagingSlip.Shipment", lang, shipment.ShipmentNumber), 14, true);
        pdf.Text(store?.CompanyName);
        pdf.Space(10);

        if (order.ShippingAddress != null)
        {
            var address = await AddressCell(order.ShippingAddress, "PDFPackagingSlip",
                "PDFInvoice.ShippingInformation", lang, null);
            foreach (var line in address.Lines) pdf.Text(line.Text, bold: line.Bold);
            pdf.Space(6);
        }

        pdf.Text(Format("PDFPackagingSlip.ShippingMethod", lang, order.ShippingMethod), bold: true);
        pdf.Space(14);
        pdf.Text(_translationService.GetResource("PDFInvoice.Product(s)", lang));
        pdf.Space(4);

        var rows = new List<IReadOnlyList<PdfCell>>();
        foreach (var item in shipment.ShipmentItems)
        {
            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            //a vendor's shipment carries only that vendor's lines
            if (orderItem == null ||
                (!string.IsNullOrEmpty(shipment.VendorId) && orderItem.VendorId != shipment.VendorId))
                continue;

            var product = await _productService.GetProductByIdIncludeArch(item.ProductId);
            var name = new List<PdfLine> { new(product?.GetTranslation(x => x.Name, lang) ?? item.ProductId) };
            var attributes = PdfHtmlText.ToPlainText(orderItem.AttributeDescription);
            if (attributes.Length > 0) name.Add(new PdfLine(attributes, Small: true));

            rows.Add([new PdfCell(name), PdfCell.Of(product?.Sku), PdfCell.Of(item.Quantity.ToString())]);
        }

        pdf.Table(
            [new PdfColumn(6), new PdfColumn(2.5), new PdfColumn(1.2, PdfAlign.Right)],
            [
                _translationService.GetResource("PDFPackagingSlip.ProductName", lang),
                _translationService.GetResource("PDFPackagingSlip.SKU", lang),
                _translationService.GetResource("PDFPackagingSlip.QTY", lang)
            ],
            rows);
    }

    private async Task<PdfCell> AddressCell(Address address, string resourcePrefix, string titleKey, string lang,
        string closingLine, IEnumerable<string> extraLines = null)
    {
        var lines = new List<PdfLine> { new(_translationService.GetResource(titleKey, lang), true) };
        if (!string.IsNullOrEmpty(address.Company))
            lines.Add(new PdfLine(Format($"{resourcePrefix}.Company", lang, address.Company)));
        lines.Add(new PdfLine(Format($"{resourcePrefix}.Name", lang, $"{address.FirstName} {address.LastName}")));
        lines.Add(new PdfLine(Format($"{resourcePrefix}.Phone", lang, address.PhoneNumber)));
        lines.Add(new PdfLine(Format($"{resourcePrefix}.Address", lang, address.Address1)));
        lines.Add(new PdfLine($"{address.City}, {address.ZipPostalCode}"));
        var country = (await _countryService.GetCountryById(address.CountryId))?.Name;
        if (!string.IsNullOrEmpty(country)) lines.Add(new PdfLine(country));
        if (!string.IsNullOrEmpty(closingLine))
        {
            lines.Add(new PdfLine(string.Empty));
            lines.Add(new PdfLine(closingLine, true));
        }

        if (extraLines != null)
            lines.AddRange(extraLines.Where(l => l.Length > 0).Select(l => new PdfLine(l)));

        return new PdfCell(lines);
    }

    /// <summary>
    ///     Loads the PDF logo as PNG, the one raster format the composer embeds. Pictures are usually stored as
    ///     WebP, so anything else is converted. A logo that cannot be read is left out rather than failing the PDF.
    /// </summary>
    private async Task<byte[]> LoadLogo()
    {
        if (string.IsNullOrEmpty(_pdfSettings.LogoPictureId)) return null;
        try
        {
            var picture = await _pictureService.GetPictureById(_pdfSettings.LogoPictureId);
            if (picture == null) return null;

            var binary = await _pictureService.LoadPictureBinary(picture);
            if (binary is not { Length: > 0 }) return null;

            return picture.MimeType?.Contains("png", StringComparison.OrdinalIgnoreCase) == true
                ? binary
                : _pictureService.ConvertPicture(binary, 100, "Png");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF logo {PictureId} could not be loaded", _pdfSettings.LogoPictureId);
            return null;
        }
    }

    private string Format(string resourceKey, string lang, object value)
    {
        return string.Format(_translationService.GetResource(resourceKey, lang), value);
    }

    private static string Money(Order order, double amount)
    {
        return $"{amount:N2} {order.CustomerCurrencyCode}";
    }
}
