using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Marketing.Documents;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.RegularExpressions;
using OperatingSystem = Grand.Infrastructure.OperatingSystem;
using Grand.Web.Common.Localization;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class DownloadController : BasePublicController
{
    private readonly CustomerSettings _customerSettings;
    private readonly IDownloadService _downloadService;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IRequestTranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGroupService _groupService;
    private readonly OrderSettings _orderSettings;

    public DownloadController(IDownloadService downloadService,
        IProductService productService,
        IOrderService orderService,
        IMerchandiseReturnService merchandiseReturnService,
        IContextAccessor contextAccessor,
        IRequestTranslationService translationService,
        CustomerSettings customerSettings,
        IGroupService groupService,
        OrderSettings orderSettings)
    {
        _downloadService = downloadService;
        _productService = productService;
        _orderService = orderService;
        _merchandiseReturnService = merchandiseReturnService;
        _contextAccessor = contextAccessor;
        _translationService = translationService;
        _customerSettings = customerSettings;
        _groupService = groupService;
        _orderSettings = orderSettings;
    }

    [HttpGet]
    public virtual async Task<IActionResult> Sample(string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            return NotFound();

        if (!product.HasSampleDownload)
            return Content("Product doesn't have a sample download.");

        var download = await _downloadService.GetDownloadById(product.SampleDownloadId);
        if (download == null)
            return Content("Sample download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    /// <summary>
    ///     Whether the current customer may open the order's files: it is theirs, or the browser holds the grant the
    ///     link sent to the order's e-mail left for it
    /// </summary>
    private async Task<bool> OrderAccess(Order order)
    {
        var (customer, _) = await GuestOrderGrant.OrderCustomer(HttpContext, order,
            _contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore.Id, _groupService);
        return customer != null;
    }

    /// <summary>
    ///     Whether the file was uploaded with the order the browser holds a grant for, whose page links it
    /// </summary>
    private async Task<bool> GrantedOrderUpload(Guid downloadGuid)
    {
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        var orderId = GuestOrderGrant.GrantedOrderId(HttpContext, storeId);
        var order = orderId != null ? await _orderService.GetOrderById(orderId) : null;
        if (await GuestOrderGrant.GrantedGuest(HttpContext, order, _contextAccessor.WorkContext.CurrentCustomer,
                storeId, _groupService) == null)
            return false;

        return order.OrderItems.SelectMany(x => x.Attributes).Concat(order.CheckoutAttributes)
            .Any(x => Guid.TryParse(x.Value, out var guid) && guid == downloadGuid);
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetDownload(Guid orderItemId, bool agree = false)
    {
        var orderItem = await _orderService.GetOrderItemByGuid(orderItemId);
        if (orderItem == null)
            return NotFound();

        var order = await _orderService.GetOrderByOrderItemId(orderItem.Id);
        var product = await _productService.GetProductById(orderItem.ProductId);
        if (!order.IsDownloadAllowed(orderItem, product))
            return Content("Downloads are not allowed");

        if (_customerSettings.DownloadableProductsValidateUser)
        {
            if (_contextAccessor.WorkContext.CurrentCustomer == null)
                return Challenge();

            if (!await OrderAccess(order))
                return Content("This is not your order");
        }

        var download = await _downloadService.GetDownloadById(product.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (product.HasUserAgreement)
            if (!agree)
                return RedirectToRoute("DownloadUserAgreement", new { orderItemId });


        if (!product.UnlimitedDownloads && orderItem.DownloadCount >= product.MaxNumberOfDownloads)
            return Content(string.Format(_translationService.GetResource("DownloadableProducts.ReachedMaximumNumber"),
                product.MaxNumberOfDownloads));


        if (download.UseDownloadUrl)
        {
            //increase download
            order.OrderItems.FirstOrDefault(x => x.Id == orderItem.Id)!.DownloadCount++;
            await _orderService.UpdateOrder(order);

            //return result
            return new RedirectResult(download.DownloadUrl);
        }

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //increase download
        order.OrderItems.FirstOrDefault(x => x.Id == orderItem.Id)!.DownloadCount++;
        await _orderService.UpdateOrder(order);

        if (product.ProductTypeId != ProductType.BundledProduct)
        {
            //return result
            var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id;
            var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
                ? download.ContentType
                : "application/octet-stream";
            return new FileContentResult(download.DownloadBinary, contentType)
                { FileDownloadName = fileName + download.Extension };
        }

        using var memoryStream = new MemoryStream();
        using (var ziparchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var fileName = (!string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id) +
                           download.Extension;
            if (OperatingSystem.IsWindows())
            {
                await System.IO.File.WriteAllBytesAsync(@"App_Data\Download\" + fileName, download.DownloadBinary);
                ziparchive.CreateEntryFromFile(@"App_Data\Download\" + fileName, fileName);
            }
            else
            {
                await System.IO.File.WriteAllBytesAsync("App_Data/Download/" + fileName, download.DownloadBinary);
                ziparchive.CreateEntryFromFile("App_Data/Download/" + fileName, fileName);
            }

            foreach (var bundle in product.BundleProducts)
            {
                var p1 = await _productService.GetProductById(bundle.ProductId);
                if (p1 is not { IsDownload: true }) continue;

                var d1 = await _downloadService.GetDownloadById(p1.DownloadId);
                if (d1 is not { UseDownloadUrl: false }) continue;
                fileName = (!string.IsNullOrWhiteSpace(d1.Filename) ? d1.Filename : p1.Id) + d1.Extension;
                if (OperatingSystem.IsWindows())
                {
                    await System.IO.File.WriteAllBytesAsync(@"App_Data\Download\" + fileName, d1.DownloadBinary);
                    ziparchive.CreateEntryFromFile(@"App_Data\Download\" + fileName, fileName);
                }
                else
                {
                    await System.IO.File.WriteAllBytesAsync("App_Data/Download/" + fileName, d1.DownloadBinary);
                    ziparchive.CreateEntryFromFile("App_Data/Download/" + fileName, fileName);
                }
            }
        }

        return File(memoryStream.ToArray(), "application/zip",
            $"{Regex.Replace(product.Name, "[^A-Za-z0-9 _]", "", RegexOptions.Compiled, TimeSpan.FromSeconds(1))}.zip");
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetLicense(Guid orderItemId)
    {
        var orderItem = await _orderService.GetOrderItemByGuid(orderItemId);
        if (orderItem == null)
            return NotFound();

        var order = await _orderService.GetOrderByOrderItemId(orderItem.Id);
        var product = await _productService.GetProductById(orderItem.ProductId);
        if (!order.IsLicenseDownloadAllowed(orderItem, product))
            return Content("Downloads are not allowed");

        if (_customerSettings.DownloadableProductsValidateUser)
            if (!await OrderAccess(order))
                return Challenge();

        var download = await _downloadService.GetDownloadById(!string.IsNullOrEmpty(orderItem.LicenseDownloadId)
            ? orderItem.LicenseDownloadId
            : "");
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : product.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetFileUpload(Guid downloadId)
    {
        var download = await _downloadService.GetDownloadByGuid(downloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (_contextAccessor.WorkContext.CurrentCustomer == null ||
            (!await _groupService.IsAdmin(_contextAccessor.WorkContext.CurrentCustomer)
             && download.CustomerId != _contextAccessor.WorkContext.CurrentCustomer.Id
             && !await GrantedOrderUpload(downloadId)))
            return Challenge();

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : downloadId.ToString();
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetOrderNoteFile(string orderNoteId)
    {
        var orderNote = await _orderService.GetOrderNote(orderNoteId);
        if (orderNote == null)
            return NotFound();

        var order = await _orderService.GetOrderById(orderNote.OrderId);
        if (order == null)
            return NotFound();

        if (!await OrderAccess(order))
            return Challenge();

        //the order's page shows the notes meant for the customer only; the staff open the others in the panels
        if (!orderNote.DisplayToCustomer)
            return NotFound();

        var download = await _downloadService.GetDownloadById(orderNote.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : orderNote.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetShipmentNoteFile(string shipmentNoteId,
        [FromServices] IShipmentService shipmentService)
    {
        var shipmentNote = await shipmentService.GetShipmentNote(shipmentNoteId);
        if (shipmentNote == null)
            return NotFound();

        var shipment = await shipmentService.GetShipmentById(shipmentNote.ShipmentId);
        if (shipment == null)
            return NotFound();

        var order = await _orderService.GetOrderById(shipment.OrderId);
        if (order == null)
            return NotFound();

        if (!await OrderAccess(order))
            return Challenge();

        //the shipment's page shows the notes meant for the customer only; the staff open the others in the panels
        if (!shipmentNote.DisplayToCustomer)
            return NotFound();

        var download = await _downloadService.GetDownloadById(shipmentNote.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : shipmentNote.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetCustomerNoteFile(string customerNoteId,
        [FromServices] ICustomerNoteService customerNoteService)
    {
        if (string.IsNullOrEmpty(customerNoteId))
            return Content("Download is not available.");

        var customerNote = await customerNoteService.GetCustomerNote(customerNoteId);
        if (customerNote == null)
            return NotFound();

        if (_contextAccessor.WorkContext.CurrentCustomer == null || customerNote.CustomerId != _contextAccessor.WorkContext.CurrentCustomer.Id)
            return Challenge();

        var download = await _downloadService.GetDownloadById(customerNote.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : customerNote.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetMerchandiseReturnNoteFile(string merchandiseReturnNoteId)
    {
        var merchandiseReturnNote = await _merchandiseReturnService.GetMerchandiseReturnNote(merchandiseReturnNoteId);
        if (merchandiseReturnNote == null)
            return NotFound();

        var merchandiseReturn =
            await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnNote.MerchandiseReturnId);
        if (merchandiseReturn == null)
            return NotFound();

        //whoever may see the return on its page: the customer, the account that owns theirs, or the guest of the
        //order who holds its grant
        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
        if ((customer == null || !await merchandiseReturn.Access(customer, _groupService)) &&
            (!GuestOrderGrant.ReturnsAllowed(_orderSettings) ||
             await GuestOrderGrant.GrantedGuest(HttpContext, order, customer,
                 _contextAccessor.StoreContext.CurrentStore.Id, _groupService) == null))
            return Challenge();

        //in the store of its order, and only a note the customer is shown
        if (order == null || order.StoreId != _contextAccessor.StoreContext.CurrentStore.Id ||
            !merchandiseReturnNote.DisplayToCustomer)
            return NotFound();

        var download = await _downloadService.GetDownloadById(merchandiseReturnNote.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : merchandiseReturnNote.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }

    [HttpGet]
    public virtual async Task<IActionResult> GetDocumentFile(string documentId,
        [FromServices] IDocumentService documentService)
    {
        if (string.IsNullOrEmpty(documentId))
            return Content("Download is not available.");

        var document = await documentService.GetById(documentId);
        if (document is not { Published: true })
            return NotFound();

        if (_contextAccessor.WorkContext.CurrentCustomer == null || document.CustomerId != _contextAccessor.WorkContext.CurrentCustomer.Id)
            return Challenge();

        var download = await _downloadService.GetDownloadById(document.DownloadId);
        if (download == null)
            return Content("Download is not available any more.");

        if (download.UseDownloadUrl)
            return new RedirectResult(download.DownloadUrl);

        //binary download
        if (download.DownloadBinary == null)
            return Content("Download data is not available any more.");

        //return result
        var fileName = !string.IsNullOrWhiteSpace(download.Filename) ? download.Filename : document.Id;
        var contentType = !string.IsNullOrWhiteSpace(download.ContentType)
            ? download.ContentType
            : "application/octet-stream";
        return new FileContentResult(download.DownloadBinary, contentType)
            { FileDownloadName = fileName + download.Extension };
    }
}