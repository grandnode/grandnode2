using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.SharedKernel.Attributes;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Common;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Common;
using Grand.Web.Models.Orders;
using Grand.Mediator;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class MerchandiseReturnController : BasePublicController
{
    #region Constructors

    public MerchandiseReturnController(
        IMerchandiseReturnService merchandiseReturnService,
        IPickupPointService pickupPointService,
        IOrderService orderService,
        IContextAccessor contextAccessor,
        IGroupService groupService,
        IRequestTranslationService translationService,
        IMediator mediator,
        IMerchandiseReturnLabelService merchandiseReturnLabelService,
        IDownloadService downloadService,
        AddressSettings addressSettings,
        OrderSettings orderSettings)
    {
        _merchandiseReturnService = merchandiseReturnService;
        _pickupPointService = pickupPointService;
        _orderService = orderService;
        _contextAccessor = contextAccessor;
        _groupService = groupService;
        _translationService = translationService;
        _mediator = mediator;
        _merchandiseReturnLabelService = merchandiseReturnLabelService;
        _downloadService = downloadService;
        _addressSettings = addressSettings;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Fields

    private const int MaxTrackingNumberLength = 100;

    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IPickupPointService _pickupPointService;
    private readonly IOrderService _orderService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGroupService _groupService;
    private readonly IRequestTranslationService _translationService;
    private readonly IMediator _mediator;
    private readonly IMerchandiseReturnLabelService _merchandiseReturnLabelService;
    private readonly IDownloadService _downloadService;
    private readonly AddressSettings _addressSettings;
    private readonly OrderSettings _orderSettings;

    #endregion

    #region Utilities

    private async Task PrepareModelAddress(AddressModel addressModel, Address address)
    {
        var countryService = HttpContext.RequestServices.GetRequiredService<ICountryService>();
        var countries =
            await countryService.GetAllCountries(_contextAccessor.WorkContext.WorkingLanguage.Id, _contextAccessor.StoreContext.CurrentStore.Id);
        addressModel = await _mediator.Send(new GetAddressModel {
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Model = addressModel,
            Address = address,
            ExcludeProperties = true,
            PrePopulateWithCustomerFields = true,
            LoadCountries = () => countries
        });
    }

    /// <summary>
    ///     An order is returned through the store it was placed in: a customer whose account every store shares
    ///     would otherwise show the order to the staff of another store
    /// </summary>
    private bool OfCurrentStore(Order order)
    {
        return order.StoreId == _contextAccessor.StoreContext.CurrentStore.Id;
    }

    private async Task<Address> PrepareAddress(MerchandiseReturnModel model)
    {
        var address = new Address();
        if (!_orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress) return address;

        if (!string.IsNullOrEmpty(model.PickupAddressId))
        {
            address = _contextAccessor.WorkContext.CurrentCustomer.Addresses.FirstOrDefault(a => a.Id == model.PickupAddressId);
        }
        else
        {
            var customAttributes = await _mediator.Send(new GetParseCustomAddressAttributes
                { SelectedAttributes = model.MerchandiseReturnNewAddress.SelectedAttributes });
            address = model.MerchandiseReturnNewAddress.ToEntity(_contextAccessor.WorkContext.CurrentCustomer, _addressSettings);
            model.NewAddressPreselected = true;
            address.Attributes = customAttributes;
        }

        return address;
    }

    /// <summary>
    ///     The return of the current customer, as the details page finds it; null when it is not theirs
    /// </summary>
    private async Task<MerchandiseReturn> CustomerMerchandiseReturn(string merchandiseReturnId)
    {
        var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (!await merchandiseReturn.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return null;

        var order = await _orderService.GetOrderById(merchandiseReturn.OrderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return null;

        return merchandiseReturn;
    }

    /// <summary>
    ///     The text of an error of a label or tracking number change: a key of MerchandiseReturnErrors, whose
    ///     storefront resource goes without the "Orders." prefix of the panels' keys
    /// </summary>
    private string LabelErrorText(string error)
    {
        const string panelPrefix = "Orders.";
        return _translationService.GetResource(error.StartsWith(panelPrefix, StringComparison.Ordinal)
            ? error[panelPrefix.Length..]
            : error);
    }

    /// <summary>
    ///     The fields posted with the form, of which the chosen return method's form is read
    /// </summary>
    private IDictionary<string, string> PostedForm()
    {
        return Request.HasFormContentType
            ? Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString())
            : new Dictionary<string, string>();
    }

    #endregion

    #region Methods

    [HttpGet]
    public virtual async Task<IActionResult> CustomerMerchandiseReturns()
    {
        if (!await _groupService.IsRegistered(_contextAccessor.WorkContext.CurrentCustomer))
            return Challenge();

        var model = await _mediator.Send(new GetMerchandiseReturns {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Store = _contextAccessor.StoreContext.CurrentStore,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Currency = _contextAccessor.WorkContext.WorkingCurrency
        });

        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> MerchandiseReturn(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService) ||
            !OfCurrentStore(order))
            return Challenge();

        if (!await _mediator.Send(new IsMerchandiseReturnAllowedQuery { Order = order }))
            return RedirectToRoute("HomePage");

        var model = await _mediator.Send(new GetMerchandiseReturn {
            Order = order,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> MerchandiseReturn(MerchandiseReturnModel model)
    {
        var order = await _orderService.GetOrderById(model.OrderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService) ||
            !OfCurrentStore(order))
            return Challenge();

        //what is left to return is checked once, when the return is saved
        if (!_orderSettings.MerchandiseReturnsEnabled || order.Deleted)
            return RedirectToRoute("HomePage");

        var address = await PrepareAddress(model);

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new MerchandiseReturnSubmitCommand
                { Address = address, Model = model, Order = order, Form = PostedForm() });
            if (result.Error == null && result.ShippingErrors.Count == 0)
            {
                //one return for each vendor of the items
                model.Result = string.Join("<br />", result.MerchandiseReturns.Select(x => string.Format(
                    _translationService.GetResource("MerchandiseReturns.Submitted"), x.ReturnNumber,
                    Url.Link("MerchandiseReturnDetails", new { merchandiseReturnId = x.Id }))));
                model.OrderNumber = order.OrderNumber;
                model.OrderCode = order.Code;
                return View(model);
            }

            //e.g. another return took the quantity in the meantime
            if (result.Error != null)
                ModelState.AddModelError("", _translationService.GetResource(result.Error));
            //resource keys, or texts the provider translated already, which are shown as they are
            foreach (var error in result.ShippingErrors)
                ModelState.AddModelError("", _translationService.GetResource(error,
                    _contextAccessor.WorkContext.WorkingLanguage.Id, error));
        }

        var returnModel = await _mediator.Send(new GetMerchandiseReturn {
            Order = order,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });
        returnModel.Error = string.Join(", ",
            ModelState.Keys.SelectMany(k => ModelState[k]!.Errors).Select(m => m.ErrorMessage).ToArray());
        returnModel.Comments = model.Comments;
        returnModel.PickupDate = model.PickupDate;
        returnModel.NewAddressPreselected = model.NewAddressPreselected;
        returnModel.MerchandiseReturnNewAddress = model.MerchandiseReturnNewAddress;
        if (returnModel.NewAddressPreselected || _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
            await PrepareModelAddress(model.MerchandiseReturnNewAddress, address);

        return View(returnModel);
    }

    [HttpGet]
    public virtual async Task<IActionResult> MerchandiseReturnDetails(string merchandiseReturnId)
    {
        var rr = await _merchandiseReturnService.GetMerchandiseReturnById(merchandiseReturnId);
        if (!await rr.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return Challenge();

        var order = await _orderService.GetOrderById(rr.OrderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return Challenge();

        var model = await _mediator.Send(new GetMerchandiseReturnDetails {
            Order = order,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            MerchandiseReturn = rr
        });

        return View(model);
    }

    /// <summary>
    ///     The return methods the customer can choose for the quantities, reasons and actions chosen in the form
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> MerchandiseReturnShippingOffers(string orderId,
        MerchandiseReturnModel model)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService) ||
            !OfCurrentStore(order))
            return NotFound("The order was not found.");

        if (!_orderSettings.MerchandiseReturnsEnabled)
            return Json(Array.Empty<ReturnShippingOfferModel>());

        var offers = await _mediator.Send(new GetMerchandiseReturnShippingOffers {
            Order = order,
            Model = model,
            PickupAddress = await PrepareAddress(model),
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            FormUrl = routeName => Url.RouteUrl(routeName)
        });
        return Json(offers);
    }

    /// <summary>
    ///     The label of a return, to print and stick on the parcel
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> MerchandiseReturnLabel(string merchandiseReturnId)
    {
        var merchandiseReturn = await CustomerMerchandiseReturn(merchandiseReturnId);
        var shipping = merchandiseReturn?.ReturnShipping;
        if (shipping?.LabelStatus != ReturnLabelStatus.Created || string.IsNullOrEmpty(shipping.LabelDownloadId))
            return NotFound("The label was not found.");

        var download = await _downloadService.GetDownloadById(shipping.LabelDownloadId);
        if (download?.DownloadBinary == null)
            return NotFound("The label was not found.");

        return File(download.DownloadBinary, download.ContentType, download.Filename + download.Extension);
    }

    /// <summary>
    ///     The tracking number of a parcel the customer sends themselves
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> MerchandiseReturnTrackingNumber(string merchandiseReturnId,
        string trackingNumber)
    {
        var merchandiseReturn = await CustomerMerchandiseReturn(merchandiseReturnId);
        if (!merchandiseReturn.CustomerEntersTrackingNumber())
            return NotFound("The return was not found.");

        trackingNumber = trackingNumber?.Trim();
        if (trackingNumber?.Length > MaxTrackingNumberLength)
            trackingNumber = trackingNumber[..MaxTrackingNumberLength];

        var error = await _merchandiseReturnLabelService.UpdateShipping(merchandiseReturn.Id,
            shipping => shipping.TrackingNumber = string.IsNullOrEmpty(trackingNumber) ? null : trackingNumber,
            //checked again on the return as stored: the staff may have received it since
            stored => stored.CustomerEntersTrackingNumber());
        if (error != null)
            Error(LabelErrorText(error));

        return RedirectToRoute("MerchandiseReturnDetails", new { merchandiseReturnId = merchandiseReturn.Id });
    }

    /// <summary>
    ///     Asks the provider again for a label it failed to create
    /// </summary>
    [HttpPost]
    public virtual async Task<IActionResult> MerchandiseReturnRetryLabel(string merchandiseReturnId)
    {
        var merchandiseReturn = await CustomerMerchandiseReturn(merchandiseReturnId);
        if (merchandiseReturn?.ReturnShipping == null)
            return NotFound("The return was not found.");

        //a click that came after the label was created has nothing left to do
        if (merchandiseReturn.ReturnShipping.LabelStatus != ReturnLabelStatus.Created)
        {
            var error = merchandiseReturn.CustomerRetriesLabel()
                ? await _merchandiseReturnLabelService.CreateLabel(merchandiseReturn.Id)
                : MerchandiseReturnErrors.LabelNotAllowed;
            if (error != null)
                Error(LabelErrorText(error));
        }

        return RedirectToRoute("MerchandiseReturnDetails", new { merchandiseReturnId = merchandiseReturn.Id });
    }

    /// <summary>
    ///     The form of the "return in store" option: the pickup points of the current store to leave the items at
    /// </summary>
    [HttpGet]
    public virtual async Task<IActionResult> InStoreReturnPoints()
    {
        var points = await _pickupPointService.LoadActivePickupPoints(_contextAccessor.StoreContext.CurrentStore.Id);
        return PartialView("Partials/InStorePoints", points.Select(x => new MerchandiseReturnInStorePointModel {
            Id = x.Id,
            Name = x.Name,
            Address = string.Join(", ", new[] { x.Address?.Address1, x.Address?.ZipPostalCode, x.Address?.City }
                .Where(part => !string.IsNullOrEmpty(part)))
        }).ToList());
    }

    #endregion
}