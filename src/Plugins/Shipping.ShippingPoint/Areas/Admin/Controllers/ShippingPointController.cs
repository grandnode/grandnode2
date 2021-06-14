using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ShippingPoint.Models;
using Shipping.ShippingPoint.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shipping.ShippingPoint.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.ShippingSettings)]
    public class ShippingPointController : BaseShippingController
    {
        private readonly IWorkContext _workContext;
        private readonly IUserFieldService _userFieldService;
        private readonly ITranslationService _translationService;
        private readonly IShippingPointService _shippingPointService;
        private readonly ICountryService _countryService;
        private readonly IStoreService _storeService;
        private readonly IPriceFormatter _priceFormatter;

        public ShippingPointController(
            IWorkContext workContext,
            IUserFieldService userFieldService,
            ITranslationService translationService,
            IShippingPointService ShippingPointService,
            ICountryService countryService,
            IStoreService storeService,
            IPriceFormatter priceFormatter
            )
        {
            _workContext = workContext;
            _userFieldService = userFieldService;
            _translationService = translationService;
            _shippingPointService = ShippingPointService;
            _countryService = countryService;
            _storeService = storeService;
            _priceFormatter = priceFormatter;
        }

        public IActionResult Configure()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var shippingPoints = await _shippingPointService.GetAllStoreShippingPoint(storeId: "", pageIndex: command.Page - 1, pageSize: command.PageSize);
            var viewModel = new List<ShippingPointModel>();

            foreach (var shippingPoint in shippingPoints)
            {
                var storeName = await _storeService.GetStoreById(shippingPoint.StoreId);
                viewModel.Add(new ShippingPointModel
                {
                    ShippingPointName = shippingPoint.ShippingPointName,
                    Description = shippingPoint.Description,
                    Id = shippingPoint.Id,
                    OpeningHours = shippingPoint.OpeningHours,
                    PickupFee = shippingPoint.PickupFee,
                    StoreName = storeName != null ? storeName.Shortcut : _translationService.GetResource("Admin.Settings.StoreScope.AllStores"),

                });
            }

            return Json(new DataSourceResult
            {
                Data = viewModel,
                Total = shippingPoints.TotalCount
            });
        }

        private async Task<ShippingPointModel> PrepareShippingPointModel(ShippingPointModel model)
        {
            model.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = string.Empty });
            foreach (var country in await _countryService.GetAllCountries(showHidden: true))
                model.AvailableCountries.Add(new SelectListItem { Text = country.Name, Value = country.Id.ToString() });
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Settings.StoreScope.AllStores"), Value = string.Empty });
            foreach (var store in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id.ToString() });
            return model;
        }

        public async Task<IActionResult> Create()
        {
            var model = new ShippingPointModel();
            await PrepareShippingPointModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ShippingPointModel model)
        {
            if (ModelState.IsValid)
            {
                var shippingPoint = model.ToEntity();
                await _shippingPointService.InsertStoreShippingPoint(shippingPoint);

                ViewBag.RefreshPage = true;
            }

            await PrepareShippingPointModel(model);

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var shippingPoints = await _shippingPointService.GetStoreShippingPointById(id);
            var model = shippingPoints.ToModel();
            await PrepareShippingPointModel(model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ShippingPointModel model)
        {
            if (ModelState.IsValid)
            {
                var shippingPoint = await _shippingPointService.GetStoreShippingPointById(model.Id);
                shippingPoint = model.ToEntity();
                await _shippingPointService.UpdateStoreShippingPoint(shippingPoint);
            }
            ViewBag.RefreshPage = true;

            await PrepareShippingPointModel(model);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var model = await _shippingPointService.GetStoreShippingPointById(id);
            await _shippingPointService.DeleteStoreShippingPoint(model);

            return new JsonResult("");
        }

    }
}
