using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Infrastructure.Security;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Grand.Mediator;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Features.Handlers.Orders;

public class
    GetMerchandiseReturnDetailsHandler : IRequestHandler<GetMerchandiseReturnDetails, MerchandiseReturnDetailsModel>
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IHtmlSanitizationService _htmlSanitizationService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<GetMerchandiseReturnDetailsHandler> _logger;
    private readonly IMediator _mediator;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly OrderSettings _orderSettings;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;
    private readonly IReturnShippingService _returnShippingService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public GetMerchandiseReturnDetailsHandler(
        IProductService productService,
        IMerchandiseReturnService merchandiseReturnService,
        IPriceFormatter priceFormatter,
        IContextAccessor contextAccessor,
        IMediator mediator,
        IDateTimeService dateTimeService,
        OrderSettings orderSettings,
        IReturnShippingService returnShippingService,
        ICurrencyService currencyService,
        ITranslationService translationService,
        ICountryService countryService,
        IHtmlSanitizationService htmlSanitizationService,
        ILogger<GetMerchandiseReturnDetailsHandler> logger)
    {
        _productService = productService;
        _merchandiseReturnService = merchandiseReturnService;
        _priceFormatter = priceFormatter;
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _dateTimeService = dateTimeService;
        _orderSettings = orderSettings;
        _returnShippingService = returnShippingService;
        _currencyService = currencyService;
        _translationService = translationService;
        _countryService = countryService;
        _htmlSanitizationService = htmlSanitizationService;
        _logger = logger;
    }

    public async Task<MerchandiseReturnDetailsModel> Handle(GetMerchandiseReturnDetails request,
        CancellationToken cancellationToken)
    {
        var model = new MerchandiseReturnDetailsModel {
            MerchandiseReturnId = request.MerchandiseReturn.Id,
            Comments = request.MerchandiseReturn.CustomerComments,
            ReturnNumber = request.MerchandiseReturn.ReturnNumber,
            ExternalId = request.MerchandiseReturn.ExternalId,
            MerchandiseReturnStatus = request.MerchandiseReturn.MerchandiseReturnStatus,
            CreatedOnUtc = request.MerchandiseReturn.CreatedOnUtc,
            ShowPickupAddress = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress,
            ShowPickupDate = _orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate,
            PickupDate = request.MerchandiseReturn.PickupDate,
            UserFields = request.MerchandiseReturn.UserFields,
            PickupAddress = await _mediator.Send(new GetAddressModel {
                Language = request.Language,
                Address = request.MerchandiseReturn.PickupAddress,
                ExcludeProperties = false
            }, cancellationToken)
        };

        foreach (var item in request.MerchandiseReturn.MerchandiseReturnItems)
        {
            var orderItem = request.Order.OrderItems.FirstOrDefault(x => x.Id == item.OrderItemId);
            if (orderItem == null)
                continue;

            var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);

            var unitPrice = _priceFormatter.FormatPrice(
                request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax
                    ?
                    //including tax
                    orderItem.UnitPriceInclTax
                    :
                    //excluding tax
                    orderItem.UnitPriceExclTax, _contextAccessor.WorkContext.WorkingCurrency);

            model.MerchandiseReturnItems.Add(new MerchandiseReturnDetailsModel.MerchandiseReturnItemModel {
                OrderItemId = item.OrderItemId,
                Quantity = item.Quantity,
                ReasonForReturn = item.ReasonForReturn,
                RequestedAction = item.RequestedAction,
                ProductName = product.GetTranslation(x => x.Name, _contextAccessor.WorkContext.WorkingLanguage.Id),
                ProductSeName = product.GetSeName(_contextAccessor.WorkContext.WorkingLanguage.Id),
                ProductPrice = unitPrice
            });
        }

        //merchandise return notes
        await PrepareMerchandiseReturnNotes(request, model);

        model.ReturnShipping = await PrepareReturnShipping(request);
        await PrepareTimeline(request, model);

        var status = request.MerchandiseReturn.MerchandiseReturnStatus;
        model.CanCancel = status == MerchandiseReturnStatus.Pending;
        model.CanPrintDocument = request.MerchandiseReturn.CustomerPrintsDocument();
        model.CanAddNote = status is MerchandiseReturnStatus.Pending or MerchandiseReturnStatus.Accepted
            or MerchandiseReturnStatus.Received;
        model.CanAttachFile = _orderSettings.MerchandiseReturns_AllowToUploadFiles;

        return model;
    }

    /// <summary>
    ///     Submitted, each status reached since, and the refund, by the dates the return records: the refund comes
    ///     before the completion it leads to
    /// </summary>
    private async Task PrepareTimeline(GetMerchandiseReturnDetails request, MerchandiseReturnDetailsModel model)
    {
        var merchandiseReturn = request.MerchandiseReturn;
        var languageId = request.Language.Id;
        var timeline = new List<MerchandiseReturnDetailsModel.TimelineEntryModel>();
        timeline.Add(new MerchandiseReturnDetailsModel.TimelineEntryModel {
            Name = _translationService.GetResource("MerchandiseReturns.Timeline.Submitted", languageId),
            Date = _dateTimeService.ConvertToUserTime(merchandiseReturn.CreatedOnUtc, DateTimeKind.Utc)
        });

        foreach (var change in merchandiseReturn.StatusHistory)
            timeline.Add(new MerchandiseReturnDetailsModel.TimelineEntryModel {
                Name = change.Status.GetTranslationEnum(_translationService, languageId),
                Date = _dateTimeService.ConvertToUserTime(change.CreatedOnUtc, DateTimeKind.Utc)
            });

        //changed before the statuses were recorded: where it stands is known, not since when
        if (merchandiseReturn.StatusHistory.Count == 0 &&
            merchandiseReturn.MerchandiseReturnStatus != MerchandiseReturnStatus.Pending)
            timeline.Add(new MerchandiseReturnDetailsModel.TimelineEntryModel {
                Name = merchandiseReturn.MerchandiseReturnStatus.GetTranslationEnum(_translationService, languageId)
            });

        var refund = merchandiseReturn.Refund;
        if (refund?.RefundedOnUtc != null)
        {
            var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
            timeline.Add(new MerchandiseReturnDetailsModel.TimelineEntryModel {
                Name = string.Format(_translationService.GetResource("MerchandiseReturns.Timeline.Refunded", languageId),
                    _priceFormatter.FormatPrice(refund.Amount, currency)),
                Date = _dateTimeService.ConvertToUserTime(refund.RefundedOnUtc.Value, DateTimeKind.Utc)
            });
        }

        //a status without a date stays last, where it was added
        foreach (var entry in timeline.OrderBy(x => x.Date == null).ThenBy(x => x.Date))
            model.Timeline.Add(entry);
    }

    private async Task<MerchandiseReturnDetailsModel.ReturnShippingModel> PrepareReturnShipping(
        GetMerchandiseReturnDetails request)
    {
        var merchandiseReturn = request.MerchandiseReturn;
        var shipping = merchandiseReturn.ReturnShipping;
        if (string.IsNullOrEmpty(shipping?.ProviderSystemName))
            return null;

        var open = merchandiseReturn.MerchandiseReturnStatus is MerchandiseReturnStatus.Pending
            or MerchandiseReturnStatus.Accepted;
        //how to send the parcel back, only while it is still to be sent with a label that was not cancelled
        var sendBack = merchandiseReturn.CustomerSendsBack();
        //the cost is kept in the primary store currency, with the rate of the order's currency
        var orderCurrency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);
        //what only the staff or the provider read (the label error, the provider's data) stays out of the model
        var model = new MerchandiseReturnDetailsModel.ReturnShippingModel {
            MethodName = shipping.OptionName,
            Cost = shipping.Cost > 0
                ? _priceFormatter.FormatPrice(shipping.Cost * shipping.CurrencyRate, orderCurrency)
                : _translationService.GetResource("MerchandiseReturns.ReturnShipping.Free", request.Language.Id),
            DropOffPointName = shipping.DropOffPoint?.Name,
            DropOffPointAddress = shipping.DropOffPoint?.Address == null
                ? null
                : await MerchandiseReturnShippingExtensions.AddressLine(_countryService,
                    shipping.DropOffPoint.Address),
            DropOffPointOpeningHours = shipping.DropOffPoint?.OpeningHours,
            Instructions = sendBack ? shipping.Instructions : null,
            ReturnCode = sendBack ? shipping.ReturnCode : null,
            //a label the carrier did not cancel is not offered once the parcel is no longer to be sent
            HasLabel = sendBack && shipping.LabelStatus == ReturnLabelStatus.Created &&
                       !string.IsNullOrEmpty(shipping.LabelDownloadId),
            LabelPending = open && shipping.LabelStatus is ReturnLabelStatus.Pending or ReturnLabelStatus.Failed,
            CanRetryLabel = merchandiseReturn.CustomerRetriesLabel(),
            TrackingNumber = shipping.TrackingNumber,
            CanEnterTrackingNumber = merchandiseReturn.CustomerEntersTrackingNumber()
        };

        if (!string.IsNullOrEmpty(shipping.TrackingNumber))
            await PrepareTrackingEvents(shipping, model);

        return model;
    }

    private async Task PrepareTrackingEvents(MerchandiseReturnShipping shipping,
        MerchandiseReturnDetailsModel.ReturnShippingModel model)
    {
        var tracker = _returnShippingService.LoadProviderBySystemName(shipping.ProviderSystemName)?.ShipmentTracker;
        if (tracker == null)
            return;

        try
        {
            var events = await tracker.GetShipmentEvents(shipping.TrackingNumber)
                .WaitAsync(TimeSpan.FromSeconds(IReturnShippingProvider.TrackingCallTimeoutSeconds));
            foreach (var shipmentEvent in events ?? [])
                model.TrackingEvents.Add(new MerchandiseReturnDetailsModel.TrackingEventModel {
                    EventName = shipmentEvent.EventName,
                    Location = shipmentEvent.Location,
                    Date = shipmentEvent.Date
                });
        }
        catch (Exception ex)
        {
            //a carrier that is down or slow leaves the page without the events, not without the page
            _logger.LogWarning(ex, "Return shipping provider {SystemName} failed to give the tracking events of " +
                                   "{TrackingNumber}", shipping.ProviderSystemName, shipping.TrackingNumber);
        }
    }

    private async Task PrepareMerchandiseReturnNotes(GetMerchandiseReturnDetails request,
        MerchandiseReturnDetailsModel model)
    {
        //oldest first, as a conversation reads
        foreach (var merchandiseReturnNote in (await _merchandiseReturnService.GetMerchandiseReturnNotes(
                     request.MerchandiseReturn.Id))
                 .Where(rrn => rrn.DisplayToCustomer)
                 .OrderBy(rrn => rrn.CreatedOnUtc)
                 .ToList())
            model.MerchandiseReturnNotes.Add(new MerchandiseReturnDetailsModel.MerchandiseReturnNote {
                Id = merchandiseReturnNote.Id,
                MerchandiseReturnId = merchandiseReturnNote.MerchandiseReturnId,
                HasDownload = !string.IsNullOrEmpty(merchandiseReturnNote.DownloadId),
                Note = NoteHtml(merchandiseReturnNote),
                CreatedByCustomer = merchandiseReturnNote.CreatedByCustomer,
                CreatedOn = _dateTimeService.ConvertToUserTime(merchandiseReturnNote.CreatedOnUtc, DateTimeKind.Utc)
            });
    }

    /// <summary>
    ///     What the customer typed is text, as the e-mails and the panels show it. A staff note is text too, unless it
    ///     is markup the rich-text allowlist keeps whole, e.g. a link to a form: such notes were shown as HTML before
    /// </summary>
    private string NoteHtml(MerchandiseReturnNote note)
    {
        if (!note.CreatedByCustomer && _htmlSanitizationService.ContainsMarkup(note.Note) &&
            !_htmlSanitizationService.ContainsDisallowedRichText(note.Note))
            return note.Note;

        return FormatText.ConvertText(note.Note);
    }
}
