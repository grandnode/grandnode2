using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Orders;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Infrastructure;
using Grand.Web.Commands.Models.Orders;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Grand.Web.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Commands.Handler.Orders;

public class MerchandiseReturnSubmitCommandHandler
    : IRequestHandler<MerchandiseReturnSubmitCommand, MerchandiseReturnSubmitResult>
{
    /// <summary>
    ///     The prefix of the fields of a return method's form
    /// </summary>
    public const string FormPrefix = "returnshipping.";

    private readonly IGroupService _groupService;
    private readonly LanguageSettings _languageSettings;
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;
    private readonly IMerchandiseReturnMessageProviderService _merchandiseReturnMessageProviderService;
    private readonly IContextAccessor _contextAccessor;
    private readonly OrderSettings _orderSettings;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IDownloadService _downloadService;
    private readonly MediaSettings _mediaSettings;
    private readonly IProductService _productService;
    private readonly ILogger<MerchandiseReturnSubmitCommandHandler> _logger;

    public MerchandiseReturnSubmitCommandHandler(IContextAccessor contextAccessor,
        IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
        IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
        IGroupService groupService,
        LanguageSettings languageSettings,
        OrderSettings orderSettings,
        IMerchandiseReturnService merchandiseReturnService,
        IDownloadService downloadService,
        MediaSettings mediaSettings,
        IProductService productService,
        ILogger<MerchandiseReturnSubmitCommandHandler> logger)
    {
        _orderSettings = orderSettings;
        _contextAccessor = contextAccessor;
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
        _merchandiseReturnMessageProviderService = merchandiseReturnMessageProviderService;
        _groupService = groupService;
        _languageSettings = languageSettings;
        _merchandiseReturnService = merchandiseReturnService;
        _downloadService = downloadService;
        _mediaSettings = mediaSettings;
        _productService = productService;
        _logger = logger;
    }

    public async Task<MerchandiseReturnSubmitResult> Handle(
        MerchandiseReturnSubmitCommand request, CancellationToken cancellationToken)
    {
        //the photos are checked before anything is saved: a refused return leaves none behind. A store that takes no
        //files asks for no photo, and saves none whatever was posted
        if (_orderSettings.MerchandiseReturns_AllowToUploadFiles)
        {
            var photoError = MerchandiseReturnUploads.PhotosError(request.Model.Items, request.Photos,
                await _merchandiseReturnService.GetAllMerchandiseReturnReasons(request.Order.StoreId),
                FileExtensions.GetAllowedMediaFileTypes(_mediaSettings.AllowedFileTypes));
            if (photoError != null)
                return new MerchandiseReturnSubmitResult { Error = photoError };
        }

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        var result = await _merchandiseReturnWorkflowService.Submit(await request.Model.ToSubmitRequest(request.Order,
            customer, _groupService, _contextAccessor.WorkContext.WorkingLanguage.Id, request.Address, request.Form,
            _orderSettings));

        //each vendor's return is a return of its own, with its own photos and messages
        foreach (var merchandiseReturn in result.MerchandiseReturns)
        {
            await SavePhotos(request, merchandiseReturn, customer.Id);
            //notify store owner here (email)
            await _merchandiseReturnMessageProviderService.SendNewMerchandiseReturnStoreOwnerMessage(merchandiseReturn,
                request.Order, _languageSettings.DefaultAdminLanguageId,
                host: _contextAccessor.StoreContext.CurrentHost);
            //notify customer
            await _merchandiseReturnMessageProviderService.SendNewMerchandiseReturnCustomerMessage(merchandiseReturn,
                request.Order, request.Order.CustomerLanguageId, host: _contextAccessor.StoreContext.CurrentHost);
        }

        return result;
    }

    /// <summary>
    ///     Saves each photo of the return's items as a note of the customer's with the photo attached, named after the
    ///     item. The return is saved already: a photo that cannot be saved is logged, and the staff can ask for it again
    /// </summary>
    private async Task SavePhotos(MerchandiseReturnSubmitCommand request, MerchandiseReturn merchandiseReturn,
        string customerId)
    {
        if (request.Photos == null || !_orderSettings.MerchandiseReturns_AllowToUploadFiles)
            return;

        foreach (var item in merchandiseReturn.MerchandiseReturnItems.Where(x =>
                     request.Photos.ContainsKey(x.OrderItemId)))
        {
            string productName = null;
            foreach (var photo in request.Photos[item.OrderItemId])
                try
                {
                    productName ??= await ProductName(request.Order, item.OrderItemId);
                    var download = MerchandiseReturnUploads.ToDownload(photo, customerId, merchandiseReturn.Id);
                    await _downloadService.InsertDownload(download);
                    await _merchandiseReturnService.InsertMerchandiseReturnNote(new MerchandiseReturnNote {
                        MerchandiseReturnId = merchandiseReturn.Id,
                        Note = productName,
                        DownloadId = download.Id,
                        DisplayToCustomer = true,
                        CreatedByCustomer = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "A photo of merchandise return {MerchandiseReturnId} was not saved",
                        merchandiseReturn.Id);
                }
        }
    }

    /// <summary>
    ///     The name of the product ordered, which a photo is named after: the form's hidden fields are the customer's to
    ///     change
    /// </summary>
    private async Task<string> ProductName(Order order, string orderItemId)
    {
        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        var product = orderItem == null ? null : await _productService.GetProductByIdIncludeArch(orderItem.ProductId);
        return product?.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id) ?? "";
    }
}
