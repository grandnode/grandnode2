using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ShippingPoint.Models;
using Shipping.ShippingPoint.Services;
using Grand.Web.Common.Localization;

namespace Shipping.ShippingPoint.Controllers;

public class SelectedShippingPointController : Controller
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IShippingPointService _shippingPointService;
    private readonly IRequestTranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IOrderService _orderService;
    private readonly IGroupService _groupService;

    public SelectedShippingPointController(
        IShippingPointService shippingPointService,
        ICountryService countryService,
        IPriceFormatter priceFormatter,
        IContextAccessor contextAccessor,
        ICurrencyService currencyService,
        IRequestTranslationService translationService,
        IOrderService orderService,
        IGroupService groupService)
    {
        _orderService = orderService;
        _groupService = groupService;
        _shippingPointService = shippingPointService;
        _countryService = countryService;
        _priceFormatter = priceFormatter;
        _contextAccessor = contextAccessor;
        _currencyService = currencyService;
        _translationService = translationService;
    }

    public async Task<IActionResult> Get(string shippingOptionId)
    {
        var shippingPoint = await _shippingPointService.GetStoreShippingPointById(shippingOptionId);
        if (shippingPoint == null) return Content("ShippingPointController: given Shipping Option doesn't exist");
        var rateBase =
            await _currencyService.ConvertFromPrimaryStoreCurrency(shippingPoint.PickupFee,
                _contextAccessor.WorkContext.WorkingCurrency);
        var fee = _priceFormatter.FormatPrice(rateBase, _contextAccessor.WorkContext.WorkingCurrency);

        var viewModel = new PointModel {
            ShippingPointName = shippingPoint.ShippingPointName,
            Description = shippingPoint.Description,
            PickupFee = fee,
            OpeningHours = shippingPoint.OpeningHours,
            Address1 = shippingPoint.Address1,
            City = shippingPoint.City,
            CountryName = (await _countryService.GetCountryById(shippingPoint.CountryId))?.Name,
            ZipPostalCode = shippingPoint.ZipPostalCode
        };
        return View(viewModel);
    }

    public async Task<IActionResult> Points(string shippingOption)
    {
        var parameter = shippingOption.Split([":"], StringSplitOptions.RemoveEmptyEntries)[0];

        if (parameter != _translationService.GetResource("Shipping.ShippingPoint.PluginName"))
            return Content("ShippingPointController: given Shipping Option doesn't exist");
        var shippingPoints = await _shippingPointService.GetAllStoreShippingPoint(_contextAccessor.StoreContext.CurrentStore.Id);

        var shippingPointsModel = new List<SelectListItem> {
            new() { Value = "", Text = _translationService.GetResource("Shipping.ShippingPoint.SelectShippingOption") }
        };
        shippingPointsModel.AddRange(shippingPoints.Select(shippingPoint => new SelectListItem
            { Value = shippingPoint.Id, Text = shippingPoint.ShippingPointName }));

        return View(shippingPointsModel);
    }

    /// <summary>
    ///     The form of the Shipping Point return option: the points of the order's store that accept returns
    /// </summary>
    /// <param name="orderId">The order the items are returned from, whose currency and rate the fees are shown in</param>
    /// <param name="free">
    ///     Whether the return is free whatever point is chosen, so no fee is shown. It only changes what is shown: the
    ///     cost saved with the return is worked out again on submit from the point chosen and the store's rules
    /// </param>
    public async Task<IActionResult> ReturnPoints(string orderId, bool free = false)
    {
        var order = string.IsNullOrEmpty(orderId) ? null : await _orderService.GetOrderById(orderId);
        if (!await CustomerOrder(order))
            return NotFound("The order was not found.");

        var points = await _shippingPointService.GetReturnShippingPoints(order.StoreId);
        //shown as the return shows its saved cost: in the order's currency, at the order's rate
        var currency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
        var model = new List<ReturnPointModel>();
        foreach (var point in points)
        {
            model.Add(new ReturnPointModel {
                Id = point.Id,
                Name = point.ShippingPointName,
                Address = await MerchandiseReturnShippingExtensions.AddressLine(_countryService, point.Address1,
                    point.ZipPostalCode, point.City, point.CountryId),
                OpeningHours = point.OpeningHours,
                ReturnFee = free
                    ? null
                    : _priceFormatter.FormatPrice(Math.Max(0, point.ReturnFee) * order.CurrencyRate, currency)
            });
        }

        return View(model);
    }

    /// <summary>
    ///     Whether the order is one the current customer returns items from, in the current store: the rule of the
    ///     storefront's return pages, which a plugin cannot reference
    /// </summary>
    private async Task<bool> CustomerOrder(Order order)
    {
        if (order == null || order.Deleted || order.StoreId != _contextAccessor.StoreContext.CurrentStore.Id)
            return false;

        var customer = _contextAccessor.WorkContext.CurrentCustomer;
        //an owner sees the orders of their sub-accounts, a sub-account only its own
        if (await _groupService.IsOwner(customer))
            return customer.Id == order.CustomerId || customer.Id == order.OwnerId;
        return customer.Id == order.CustomerId;
    }
}