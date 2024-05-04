using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ShippingPoint.Models;
using Shipping.ShippingPoint.Services;

namespace Shipping.ShippingPoint.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingPointController : BaseShippingController
{
    private readonly ICountryService _countryService;
    private readonly IShippingPointService _shippingPointService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;

    public ShippingPointController(
        ITranslationService translationService,
        IShippingPointService shippingPointService,
        ICountryService countryService,
        IStoreService storeService
    )
    {
        _translationService = translationService;
        _shippingPointService = shippingPointService;
        _countryService = countryService;
        _storeService = storeService;
    }

    public IActionResult Configure()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        var shippingPoints = await _shippingPointService.GetAllStoreShippingPoint("",
            command.Page - 1, command.PageSize);
        var viewModel = new List<ShippingPointModel>();

        foreach (var shippingPoint in shippingPoints)
        {
            var storeName = await _storeService.GetStoreById(shippingPoint.StoreId);
            viewModel.Add(new ShippingPointModel {
                ShippingPointName = shippingPoint.ShippingPointName,
                Description = shippingPoint.Description,
                Id = shippingPoint.Id,
                OpeningHours = shippingPoint.OpeningHours,
                PickupFee = shippingPoint.PickupFee,
                StoreName = storeName != null
                    ? storeName.Shortcut
                    : _translationService.GetResource("Admin.Settings.StoreScope.AllStores")
            });
        }

        return Json(new DataSourceResult {
            Data = viewModel,
            Total = shippingPoints.TotalCount
        });
    }

    private async Task PrepareShippingPointModel(ShippingPointModel model)
    {
        model.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = string.Empty });
        foreach (var country in await _countryService.GetAllCountries(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = country.Name, Value = country.Id });
        model.AvailableStores.Add(new SelectListItem {
            Text = _translationService.GetResource("Admin.Settings.StoreScope.AllStores"), Value = string.Empty
        });
        foreach (var store in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = store.Shortcut, Value = store.Id });
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
            return Content("");
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
            var shippingPoint = model.ToEntity();
            await _shippingPointService.UpdateStoreShippingPoint(shippingPoint);

            return Content("");
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