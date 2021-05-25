using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Pdf;
using Grand.Domain.Data;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Wkhtmltopdf.NetCore;

namespace Grand.Business.Common.Services.Pdf
{
    /// <summary>
    /// Generate invoice  products , shipment as pdf (from html template to pdf)
    /// </summary>
    public class WkPdfService : IPdfService
    {
        private const string _orderTemaplate = "~/Views/PdfTemplates/OrderPdfTemplate.cshtml";
        private const string _shipmentsTemaplate = "~/Views/PdfTemplates/ShipmentPdfTemplate.cshtml";
        private const string _orderFooter = "pdf/footers/order.html";
        private const string _shipmentFooter = "pdf/footers/shipment.html";
        private readonly IGeneratePdf _generatePdf;
        private readonly IViewRenderService _viewRenderService;
        private readonly ILanguageService _languageService;
        private readonly IRepository<Download> _downloadRepository;
        private readonly IDatabaseContext _dbContext;

        public WkPdfService(IGeneratePdf generatePdf, IViewRenderService viewRenderService, IRepository<Download> downloadRepository,
            ILanguageService languageService, IDatabaseContext dbContext)
        {
            _generatePdf = generatePdf;
            _viewRenderService = viewRenderService;
            _languageService = languageService;
            _downloadRepository = downloadRepository;
            _dbContext = dbContext;
        }

        public async Task PrintOrdersToPdf(Stream stream, IList<Order> orders, string languageId = "", string vendorId = "")
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (orders == null)
                throw new ArgumentNullException(nameof(orders));

            _generatePdf.SetConvertOptions(new ConvertOptions()
            {
                PageSize = Wkhtmltopdf.NetCore.Options.Size.A4,
                PageMargins = new Wkhtmltopdf.NetCore.Options.Margins() { Bottom = 10, Left = 10, Right = 10, Top = 10 },
                FooterHtml = CommonPath.WebMapPath(_orderFooter)
            });

            var html = await _viewRenderService.RenderToStringAsync<(IList<Order>,string)>(_orderTemaplate, new (orders, vendorId));
            var pdfBytes = _generatePdf.GetPDF(html);
            stream.Write(pdfBytes);
        }

        public async Task<string> PrintOrderToPdf(Order order, string languageId, string vendorId = "")
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var fileName = string.Format("order_{0}_{1}.pdf", order.OrderGuid, CommonHelper.GenerateRandomDigitCode(4));
            var filePath = Path.Combine(CommonPath.WebMapPath("assets/files/exportimport"), fileName);
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

            _generatePdf.SetConvertOptions(new ConvertOptions()
            {
                PageSize = Wkhtmltopdf.NetCore.Options.Size.A4,
                PageMargins = new Wkhtmltopdf.NetCore.Options.Margins() { Bottom = 10, Left = 10, Right = 10, Top = 10 },
                FooterHtml = CommonPath.WebMapPath(_shipmentFooter)
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
                var download = new Download
                {
                    Filename = fileName,
                    Extension = ".pdf",
                    ContentType = "application/pdf",
                };

                download.DownloadObjectId = await _dbContext.GridFSBucketUploadFromBytesAsync(download.Filename, ms.ToArray());
                await _downloadRepository.InsertAsync(download);

                //TODO
                //await _mediator.EntityInserted(download);
                downloadId = download.Id;
            }
            return downloadId;
        }
    }

}
