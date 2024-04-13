using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ShippingPoint.Models;
using Shipping.ShippingPoint.Services;

namespace Shipping.ShippingPoint.Controllers;

public class SelectedShippingPointController : Controller
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IShippingPointService _shippingPointService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public SelectedShippingPointController(
        IShippingPointService shippingPointService,
        ICountryService countryService,
        IPriceFormatter priceFormatter,
        IWorkContext workContext,
        ICurrencyService currencyService,
        ITranslationService translationService)
    {
        _shippingPointService = shippingPointService;
        _countryService = countryService;
        _priceFormatter = priceFormatter;
        _workContext = workContext;
        _currencyService = currencyService;
        _translationService = translationService;
    }

    public async Task<IActionResult> Get(string shippingOptionId)
    {
        var shippingPoint = await _shippingPointService.GetStoreShippingPointById(shippingOptionId);
        if (shippingPoint == null) return Content("ShippingPointController: given Shipping Option doesn't exist");
        var rateBase =
            await _currencyService.ConvertFromPrimaryStoreCurrency(shippingPoint.PickupFee,
                _workContext.WorkingCurrency);
        var fee = _priceFormatter.FormatPrice(rateBase, _workContext.WorkingCurrency);

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
        var shippingPoints = await _shippingPointService.GetAllStoreShippingPoint(_workContext.CurrentStore.Id);

        var shippingPointsModel = new List<SelectListItem> {
            new() { Value = "", Text = _translationService.GetResource("Shipping.ShippingPoint.SelectShippingOption") }
        };
        shippingPointsModel.AddRange(shippingPoints.Select(shippingPoint => new SelectListItem
            { Value = shippingPoint.Id, Text = shippingPoint.ShippingPointName }));

        return View(shippingPointsModel);
    }
}