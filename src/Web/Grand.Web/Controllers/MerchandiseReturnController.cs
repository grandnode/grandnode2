using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Queries.Checkout.Orders;
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
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Controllers;

[DenySystemAccount]
[ApiGroup(SharedKernel.Extensions.ApiConstants.ApiGroupNameV2)]
public class MerchandiseReturnController : BasePublicController
{
    #region Constructors

    public MerchandiseReturnController(
        IMerchandiseReturnService merchandiseReturnService,
        IOrderService orderService,
        IContextAccessor contextAccessor,
        IGroupService groupService,
        ITranslationService translationService,
        IMediator mediator,
        AddressSettings addressSettings,
        OrderSettings orderSettings)
    {
        _merchandiseReturnService = merchandiseReturnService;
        _orderService = orderService;
        _contextAccessor = contextAccessor;
        _groupService = groupService;
        _translationService = translationService;
        _mediator = mediator;
        _addressSettings = addressSettings;
        _orderSettings = orderSettings;
    }

    #endregion

    #region Fields

    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IOrderService _orderService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IMediator _mediator;
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
            Language = _contextAccessor.WorkContext.WorkingLanguage
        });

        return View(model);
    }

    [HttpGet]
    public virtual async Task<IActionResult> MerchandiseReturn(string orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
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
    [AutoValidateAntiforgeryToken]
    public virtual async Task<IActionResult> MerchandiseReturn(MerchandiseReturnModel model)
    {
        var order = await _orderService.GetOrderById(model.OrderId);
        if (!await order.Access(_contextAccessor.WorkContext.CurrentCustomer, _groupService))
            return Challenge();

        if (!await _mediator.Send(new IsMerchandiseReturnAllowedQuery { Order = order }))
            return RedirectToRoute("HomePage");

        var address = await PrepareAddress(model);

        if (ModelState.IsValid)
        {
            var result = await _mediator.Send(new MerchandiseReturnSubmitCommand
                { Address = address, Model = model, Order = order });

            model.Result = string.Format(_translationService.GetResource("MerchandiseReturns.Submitted"),
                result.ReturnNumber,
                Url.Link("MerchandiseReturnDetails", new { merchandiseReturnId = result.Id }));
            model.OrderNumber = order.OrderNumber;
            model.OrderCode = order.Code;
            return View(model);
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

    #endregion
}