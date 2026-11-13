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
using Grand.Web.Models.Orders;
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
        _logger = logger;
    }

    public async Task<MerchandiseReturnSubmitResult> Handle(
        MerchandiseReturnSubmitCommand request, CancellationToken cancellationToken)
    {
        //the photos are checked before anything is saved: a refused return leaves none behind
        var photoError = MerchandiseReturnUploads.PhotosError(request.Model.Items, request.Photos,
            await _merchandiseReturnService.GetAllMerchandiseReturnReasons(request.Order.StoreId),
            FileExtensions.GetAllowedMediaFileTypes(_mediaSettings.AllowedFileTypes));
        if (photoError != null)
            return new MerchandiseReturnSubmitResult { Error = photoError };

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        var result = await _merchandiseReturnWorkflowService.Submit(new MerchandiseReturnSubmitRequest {
            Order = request.Order,
            //the store the order was placed in, whose staff handle its returns
            StoreId = request.Order.StoreId,
            CustomerId = customer.Id,
            OwnerId = await _groupService.IsOwner(customer) ? customer.Id : customer.OwnerId,
            LanguageId = _contextAccessor.WorkContext.WorkingLanguage.Id,
            Comments = request.Model.Comments,
            PickupAddress = request.Address,
            PickupDate = request.Model.PickupDate,
            //a store that does not ask for the requested action saves none, whatever was posted
            Items = SubmitItems(request.Model, _orderSettings),
            ReturnShipping = ReturnShippingChoice(request.Model, request.Form)
        });

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
        foreach (var item in merchandiseReturn.MerchandiseReturnItems)
        {
            if (request.Photos == null || !request.Photos.TryGetValue(item.OrderItemId, out var photos))
                continue;

            foreach (var photo in photos)
                try
                {
                    var download = MerchandiseReturnUploads.ToDownload(photo, customerId, merchandiseReturn.Id);
                    await _downloadService.InsertDownload(download);
                    await _merchandiseReturnService.InsertMerchandiseReturnNote(new MerchandiseReturnNote {
                        MerchandiseReturnId = merchandiseReturn.Id,
                        Note = request.Model.Items.FirstOrDefault(x => x.Id == item.OrderItemId)?.ProductName ?? "",
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
    ///     The items of the form as the return is asked for: a store that does not ask for the requested action saves
    ///     none, whatever was posted. The estimate of the form asks for the same
    /// </summary>
    internal static IList<MerchandiseReturnSubmitItem> SubmitItems(MerchandiseReturnModel model,
        OrderSettings orderSettings)
    {
        return model.Items.Select(x => new MerchandiseReturnSubmitItem(x.Id, x.Quantity,
            x.MerchandiseReturnReasonId,
            orderSettings.MerchandiseReturns_HideRequestedAction ? null : x.MerchandiseReturnActionId)).ToList();
    }

    /// <summary>
    ///     The return method chosen, with the fields of its form, prefix stripped; null when none was chosen
    /// </summary>
    internal static ReturnShippingChoice ReturnShippingChoice(MerchandiseReturnModel model,
        IDictionary<string, string> postedForm)
    {
        if (string.IsNullOrEmpty(model.ReturnShippingProvider))
            return null;

        var form = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in postedForm ?? new Dictionary<string, string>())
            if (key.StartsWith(FormPrefix, StringComparison.OrdinalIgnoreCase) && key.Length > FormPrefix.Length)
                form[key[FormPrefix.Length..]] = value;

        return new ReturnShippingChoice {
            ProviderSystemName = model.ReturnShippingProvider,
            OptionCode = model.ReturnShippingOption,
            Form = form
        };
    }
}
