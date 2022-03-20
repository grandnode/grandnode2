using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shipping.ShippingPoint.Models;
using Shipping.ShippingPoint.Services;

namespace Shipping.ShippingPoint.Controllers
{
    public class SelectedShippingPointController : Controller
    {
        private readonly IShippingPointService _shippingPointService;
        private readonly ICountryService _countryService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IWorkContext _workContext;
        private readonly ICurrencyService _currencyService;

        public SelectedShippingPointController(
            IShippingPointService shippingPointService,
            ICountryService countryService,
            IPriceFormatter priceFormatter,
            IWorkContext workContext,
            ICurrencyService currencyService)
        {
            _shippingPointService = shippingPointService;
            _countryService = countryService;
            _priceFormatter = priceFormatter;
            _workContext = workContext;
            _currencyService = currencyService;
        }
        public async Task<IActionResult> Get(string shippingOptionId)
        {
            var shippingPoint = await _shippingPointService.GetStoreShippingPointById(shippingOptionId);
            if (shippingPoint != null)
            {
                double rateBase = await _currencyService.ConvertFromPrimaryStoreCurrency(shippingPoint.PickupFee, _workContext.WorkingCurrency);
                var fee = _priceFormatter.FormatShippingPrice(rateBase);

                var viewModel = new PointModel() {
                    ShippingPointName = shippingPoint.ShippingPointName,
                    Description = shippingPoint.Description,
                    PickupFee = fee,
                    OpeningHours = shippingPoint.OpeningHours,
                    Address1 = shippingPoint.Address1,
                    City = shippingPoint.City,
                    CountryName = (await _countryService.GetCountryById(shippingPoint.CountryId))?.Name,
                    ZipPostalCode = shippingPoint.ZipPostalCode,
                };
                return View(viewModel);
            }
            return Content("ShippingPointController: given Shipping Option doesn't exist");
        }
    }
}
