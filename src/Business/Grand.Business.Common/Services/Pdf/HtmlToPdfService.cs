using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Data;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.SharedKernel.Extensions;
using Scryber;
using Scryber.Components;
using Path = System.IO.Path;

namespace Grand.Business.Common.Services.Pdf;

/// <summary>
///     Generate invoice, shipment as pdf (from html template to pdf)
/// </summary>
public class HtmlToPdfService : IPdfService
{
    private const string OrderTemplate = "~/Views/PdfTemplates/OrderPdfTemplate.cshtml";
    private const string ShipmentsTemplate = "~/Views/PdfTemplates/ShipmentPdfTemplate.cshtml";
    private readonly IRepository<Download> _downloadRepository;
    private readonly ILanguageService _languageService;
    private readonly IStoreFilesContext _storeFilesContext;

    private readonly IViewRenderService _viewRenderService;

    public HtmlToPdfService(IViewRenderService viewRenderService, IRepository<Download> downloadRepository,
        ILanguageService languageService, IStoreFilesContext storeFilesContext)
    {
        _viewRenderService = viewRenderService;
        _languageService = languageService;
        _downloadRepository = downloadRepository;
        _storeFilesContext = storeFilesContext;
    }

    public async Task PrintOrdersToPdf(Stream stream, IList<Order> orders, string languageId = "",
        string vendorId = "")
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(orders);

        var html = await _viewRenderService.RenderToStringAsync(OrderTemplate,
            new ValueTuple<IList<Order>, string>(orders, vendorId));
        TextReader sr = new StringReader(html);
        using var doc = Document.ParseDocument(sr, ParseSourceType.DynamicContent);
        doc.SaveAsPDF(stream);
    }

    public async Task<string> PrintOrderToPdf(Order order, string languageId, string vendorId = "")
    {
        ArgumentNullException.ThrowIfNull(order);

        var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}.pdf";

        var dir = CommonPath.WebMapPath("assets/files/exportimport");
        if (dir == null)
            throw new ArgumentNullException(nameof(dir));

        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

        var filePath = Path.Combine(dir, fileName);
        await using var fileStream = new FileStream(filePath, FileMode.Create);
        var orders = new List<Order> {
            order
        };
        await PrintOrdersToPdf(fileStream, orders, languageId, vendorId);
        return filePath;
    }

    public async Task PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, string languageId = "")
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(shipments);

        var lang = await _languageService.GetLanguageById(languageId);
        if (lang == null)
            throw new ArgumentException($"Cannot load language. ID={languageId}");

        var html = await _viewRenderService.RenderToStringAsync(ShipmentsTemplate, shipments);
        TextReader sr = new StringReader(html);
        using var doc = Document.ParseDocument(sr, ParseSourceType.DynamicContent);
        doc.SaveAsPDF(stream);
    }

    public async Task<string> SaveOrderToBinary(Order order, string languageId, string vendorId = "")
    {
        ArgumentNullException.ThrowIfNull(order);

        var fileName = $"order_{order.OrderGuid}_{CommonHelper.GenerateRandomDigitCode(4)}";
        using var ms = new MemoryStream();
        var orders = new List<Order> {
            order
        };
        await PrintOrdersToPdf(ms, orders, languageId, vendorId);
        var download = new Download {
            Filename = fileName,
            Extension = ".pdf",
            ContentType = "application/pdf"
        };

        download.DownloadObjectId = await _storeFilesContext.BucketUploadFromBytes(download.Filename, ms.ToArray());
        await _downloadRepository.InsertAsync(download);

        //await _mediator.EntityInserted(download);
        return download.Id;
    }
}