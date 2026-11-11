using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The places in the warehouses where batches and serial numbers are kept. Store manages the locations of its own
///     warehouses
/// </summary>
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
[AutoValidateAntiforgeryToken]
public abstract class BaseWarehouseLocationController(
    IWarehouseLocationService locationService,
    IWarehouseService warehouseService,
    IRequestTranslationService translationService,
    IAdminDataScope<Warehouse> warehouseScope) : BaseController
{
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var model = new WarehouseLocationListModel();
        model.AvailableWarehouses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var warehouse in await Warehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id });
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> LocationList(DataSourceRequest command, WarehouseLocationListModel model)
    {
        var warehouses = (await Warehouses()).ToDictionary(x => x.Id);
        var warehouseIds = string.IsNullOrEmpty(model.SearchWarehouseId)
            ? warehouses.Keys.ToList()
            : warehouses.Keys.Where(x => x == model.SearchWarehouseId).ToList();
        var locations = await locationService.GetLocations(warehouseIds, command.Page - 1, command.PageSize);
        return Json(new DataSourceResult {
            Data = locations.Select(x => new WarehouseLocationModel {
                Id = x.Id, Code = x.Code, Name = x.Name, DisplayOrder = x.DisplayOrder,
                Warehouse = warehouses.GetValueOrDefault(x.WarehouseId)?.Name
            }).ToList(),
            Total = locations.TotalCount
        });
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpGet]
    public async Task<IActionResult> Create(string warehouseId)
    {
        var model = new WarehouseLocationModel { WarehouseId = warehouseId };
        await PrepareModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Create(WarehouseLocationModel model, bool continueEditing)
    {
        if (ModelState.IsValid && !await CanUse(model.WarehouseId))
            ModelState.AddModelError(nameof(model.WarehouseId),
                translationService.GetResource("Admin.Configuration.Shipping.WarehouseLocations.Fields.Warehouse.Required"));
        await CheckCodeFree(model);
        if (!ModelState.IsValid)
        {
            await PrepareModel(model);
            return View(model);
        }

        var location = Apply(new WarehouseLocation(), model);
        await locationService.InsertLocation(location);
        Success(translationService.GetResource("Admin.Configuration.Shipping.WarehouseLocations.Added"));
        return continueEditing ? RedirectToAction("Edit", new { id = location.Id }) : RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var location = await locationService.GetLocationById(id);
        if (location == null || !await CanUse(location.WarehouseId))
            return RedirectToAction("List");

        var model = new WarehouseLocationModel {
            Id = location.Id, WarehouseId = location.WarehouseId, Code = location.Code, Name = location.Name,
            DisplayOrder = location.DisplayOrder
        };
        await PrepareModel(model);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public async Task<IActionResult> Edit(WarehouseLocationModel model, bool continueEditing)
    {
        var location = await locationService.GetLocationById(model.Id);
        if (location == null || !await CanUse(location.WarehouseId))
            return RedirectToAction("List");
        //the units kept there stay in its warehouse: a location never moves to another one
        model.WarehouseId = location.WarehouseId;
        await CheckCodeFree(model);
        if (!ModelState.IsValid)
        {
            await PrepareModel(model);
            return View(model);
        }

        await locationService.UpdateLocation(Apply(location, model));
        Success(translationService.GetResource("Admin.Configuration.Shipping.WarehouseLocations.Updated"));
        return continueEditing ? RedirectToAction("Edit", new { id = location.Id }) : RedirectToAction("List");
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var location = await locationService.GetLocationById(id);
        if (location == null || !await CanUse(location.WarehouseId))
            return RedirectToAction("List");

        await locationService.DeleteLocation(location);
        Success(translationService.GetResource("Admin.Configuration.Shipping.WarehouseLocations.Deleted"));
        return RedirectToAction("List");
    }

    /// <summary>
    ///     The code is what staff read on the shelf: two places of one warehouse never share it
    /// </summary>
    private async Task CheckCodeFree(WarehouseLocationModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code) || string.IsNullOrEmpty(model.WarehouseId))
            return;
        var code = model.Code.Trim();
        if ((await locationService.GetLocations([model.WarehouseId])).Any(x =>
                x.Id != model.Id && string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(model.Code),
                translationService.GetResource("Admin.Configuration.Shipping.WarehouseLocations.Fields.Code.Unique"));
    }

    private static WarehouseLocation Apply(WarehouseLocation location, WarehouseLocationModel model)
    {
        location.WarehouseId = model.WarehouseId;
        location.Code = model.Code?.Trim();
        location.Name = model.Name?.Trim();
        location.DisplayOrder = model.DisplayOrder;
        return location;
    }

    /// <summary>
    ///     The warehouses whose locations are managed here: all of them, or a store's own
    /// </summary>
    private async Task<IList<Warehouse>> Warehouses()
    {
        return await warehouseService.GetAllWarehouses(await warehouseScope.GetDefaultStoreIdAsync() ?? "");
    }

    private async Task<bool> CanUse(string warehouseId)
    {
        var warehouse = string.IsNullOrEmpty(warehouseId) ? null : await warehouseService.GetWarehouseById(warehouseId);
        return warehouse != null && await warehouseScope.HasAccess(warehouse);
    }

    private async Task PrepareModel(WarehouseLocationModel model)
    {
        model.AvailableWarehouses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var warehouse in await Warehouses())
            model.AvailableWarehouses.Add(new SelectListItem {
                Text = warehouse.Name, Value = warehouse.Id, Selected = warehouse.Id == model.WarehouseId
            });
    }
}
