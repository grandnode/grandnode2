using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Shipping.ShippingPoint.Models;
using Shipping.ShippingPoint.Services;

namespace Shipping.ShippingPoint.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the shipping point (pickup point) provider.
///     A store owner can add, edit and delete pickup points, but only the ones that belong
///     to his own store (<see cref="CurrentStoreId" />). Points owned by other stores or
///     global (store = *) points are never returned nor mutated.
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ShippingPointController : BaseController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ICountryService _countryService;
    private readonly IShippingPointService _shippingPointService;
    private readonly ITranslationService _translationService;

    public ShippingPointController(
        ITranslationService translationService,
        IShippingPointService shippingPointService,
        ICountryService countryService,
        IContextAccessor contextAccessor
    )
    {
        _translationService = translationService;
        _shippingPointService = shippingPointService;
        _countryService = countryService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current staff/store-manager is bound to.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public IActionResult Configure()
    {
        return View();
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> List(DataSourceRequest command)
    {
        //only the current store points - filtered and paged in the service layer
        var shippingPoints = await _shippingPointService.GetAllStoreShippingPoint(CurrentStoreId,
            command.Page - 1, command.PageSize);

        var viewModel = shippingPoints.Select(shippingPoint => new ShippingPointModel {
            ShippingPointName = shippingPoint.ShippingPointName,
            Description = shippingPoint.Description,
            Id = shippingPoint.Id,
            OpeningHours = shippingPoint.OpeningHours,
            PickupFee = shippingPoint.PickupFee
        }).ToList();

        return Json(new DataSourceResult {
            Data = viewModel,
            Total = shippingPoints.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create()
    {
        var model = new ShippingPointModel {
            //the owner cannot create points for another store
            StoreId = CurrentStoreId
        };
        await PrepareShippingPointModel(model);
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public async Task<IActionResult> Create(ShippingPointModel model)
    {
        if (ModelState.IsValid)
        {
            var shippingPoint = model.ToEntity();
            //force the current store - the owner cannot create points for another store
            shippingPoint.StoreId = CurrentStoreId;
            await _shippingPointService.InsertStoreShippingPoint(shippingPoint);

            ViewBag.RefreshPage = true;
            return Content("");
        }

        await PrepareShippingPointModel(model);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var shippingPoint = await _shippingPointService.GetStoreShippingPointById(id);
        //guard: a store owner can only open his own store points
        if (shippingPoint == null || shippingPoint.StoreId != CurrentStoreId)
            return RedirectToAction("Configure");

        var model = shippingPoint.ToModel();
        await PrepareShippingPointModel(model);
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> Edit(ShippingPointModel model)
    {
        var existing = await _shippingPointService.GetStoreShippingPointById(model.Id);
        //guard: a store owner can only edit his own store points
        if (existing == null || existing.StoreId != CurrentStoreId)
            return RedirectToAction("Configure");

        if (ModelState.IsValid)
        {
            var shippingPoint = model.ToEntity();
            //StoreId is deliberately not taken from the model - the point stays in the owner's store
            shippingPoint.StoreId = existing.StoreId;
            await _shippingPointService.UpdateStoreShippingPoint(shippingPoint);

            return Content("");
        }

        ViewBag.RefreshPage = true;

        await PrepareShippingPointModel(model);

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(string id)
    {
        var shippingPoint = await _shippingPointService.GetStoreShippingPointById(id);
        //guard: a store owner can only delete his own store points
        if (shippingPoint == null || shippingPoint.StoreId != CurrentStoreId)
            return new JsonResult("");

        await _shippingPointService.DeleteStoreShippingPoint(shippingPoint);

        return new JsonResult("");
    }

    /// <summary>
    ///     Fills the drop-downs of the add/edit form. No store selector is offered - the point
    ///     always belongs to the owner's own store.
    /// </summary>
    [NonAction]
    private async Task PrepareShippingPointModel(ShippingPointModel model)
    {
        model.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = string.Empty });
        foreach (var country in await _countryService.GetAllCountries(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = country.Name, Value = country.Id });
    }
}
