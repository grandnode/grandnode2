using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[Area(Constants.AreaStore)]
[AuthorizeStore]
[AutoValidateAntiforgeryToken]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
[Route("[area]/Shipping/[action]/{id?}")]
public class WarehouseController(
    IWarehouseService warehouseService,
    ICountryService countryService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.Warehouse> scope)
    : BaseWarehouseController(warehouseService, countryService, storeService, translationService, scope)
{
    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult Warehouses() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> WarehousesListData() => WarehouseListCore();

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> CreateWarehouse() => WarehouseCreateGetCore();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreateWarehouse(WarehouseModel model, bool continueEditing) =>
        WarehouseCreatePostCore(model, continueEditing);

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> EditWarehouse(string id) => WarehouseEditGetCore(id);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditWarehouse(WarehouseModel model, bool continueEditing) =>
        WarehouseEditPostCore(model, continueEditing);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public Task<IActionResult> DeleteWarehouse(string id) => WarehouseDeleteCore(id);
}
