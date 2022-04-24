using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Domain.Data;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.SharedKernel.Extensions;
using Wkhtmltopdf.NetCore;

namespace Grand.Business.Common.Services.Pdf
{
    /// <summary>
    /// Generate invoice, shipment as pdf (from html template to pdf)
    /// </summary>
    public class WkPdfService : IPdfService
    {
        private const string _orderTemaplate = "~/Views/PdfTemplates/OrderPdfTemplate.cshtml";
        private const string _shipmentsTemaplate = "~/Views/PdfTemplates/ShipmentPdfTemplate.cshtml";
        private const string _paginationFooter = "assets/pdf/pagination.html";

        private readonly IGeneratePdf _generatePdf;
        private readonly IViewRenderService _viewRenderService;
        private readonly ILanguageService _languageService;
        private readonly IRepository<Download> _downloadRepository;
        private readonly IStoreFilesContext _storeFilesContext;

        public WkPdfService(IGeneratePdf generatePdf, IViewRenderService viewRenderService, IRepository<Download> downloadRepository,
            ILanguageService languageService, IStoreFilesContext storeFilesContext)
        {
            _generatePdf = generatePdf;
            _viewRenderService = viewRenderService;
            _languageService = languageService;
            _downloadRepository = downloadRepository;
            _storeFilesContext = storeFilesContext;
        }

        public async Task PrintOrdersToPdf(Stream stream, IList<Order> orders, string languageId = "", string vendorId = "")
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            _generatePdf.SetConvertOptions(new ConvertOptions() {
                PageSize = Wkhtmltopdf.NetCore.Options.Size.A4,
                PageMargins = new Wkhtmltopdf.NetCore.Options.Margins() { Bottom = 10, Left = 10, Right = 10, Top = 10 },
                FooterHtml = CommonPath.WebHostMapPath(_paginationFooter)
            });

            var html = await _viewRenderService.RenderToStringAsync<(IList<Order>, string)>(_orderTemaplate, new(orders, vendorId));
            var pdfBytes = _generatePdf.GetPDF(html);
            stream.Write(pdfBytes);
        }

        public async Task<string> PrintOrderToPdf(Order order, string languageId, string vendorId = "")
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var fileName = string.Format("order_{0}_{1}.pdf", order.OrderGuid, CommonHelper.GenerateRandomDigitCode(4));

            var dir = CommonPath.WebMapPath("assets/files/exportimport");
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            var filePath = Path.Combine(dir, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                var orders = new List<Order>
                {
                    order
                };
                await PrintOrdersToPdf(fileStream, orders, languageId, vendorId);
            }
            return filePath;
        }

        public async Task PrintPackagingSlipsToPdf(Stream stream, IList<Shipment> shipments, string languageId = "")
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (shipments == null)
                throw new ArgumentNullException(nameof(shipments));

            var lang = await _languageService.GetLanguageById(languageId);
            if (lang == null)
                throw new ArgumentException(string.Format("Cannot load language. ID={0}", languageId));

            _generatePdf.SetConvertOptions(new ConvertOptions() {
                PageSize = Wkhtmltopdf.NetCore.Options.Size.A4,
                PageMargins = new Wkhtmltopdf.NetCore.Options.Margins() { Bottom = 10, Left = 10, Right = 10, Top = 10 },
                FooterHtml = CommonPath.WebHostMapPath(_paginationFooter)
            });

            var html = await _viewRenderService.RenderToStringAsync<IList<Shipment>>(_shipmentsTemaplate, shipments);
            var pdfBytes = _generatePdf.GetPDF(html);
            stream.Write(pdfBytes);

        }

        public async Task<string> SaveOrderToBinary(Order order, string languageId, string vendorId = "")
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            string fileName = string.Format("order_{0}_{1}", order.OrderGuid, CommonHelper.GenerateRandomDigitCode(4));
            string downloadId = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                var orders = new List<Order>
                {
                    order
                };
                await PrintOrdersToPdf(ms, orders, languageId, vendorId);
                var download = new Download {
                    Filename = fileName,
                    Extension = ".pdf",
                    ContentType = "application/pdf",
                };

                download.DownloadObjectId = await _storeFilesContext.BucketUploadFromBytes(download.Filename, ms.ToArray());
                await _downloadRepository.InsertAsync(download);

                //TODO
                //await _mediator.EntityInserted(download);
                downloadId = download.Id;
            }
            return downloadId;
        }
    }

}
