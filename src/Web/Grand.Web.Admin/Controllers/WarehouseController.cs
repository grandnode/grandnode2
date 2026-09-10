using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Admin.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[Area(Constants.AreaAdmin)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
[Route("[area]/Shipping/[action]")]
public class WarehouseController(
    IWarehouseService warehouseService,
    ICountryService countryService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.Warehouse> scope)
    : BaseWarehouseController(warehouseService, countryService, storeService, translationService, scope)
{
    [HttpGet]
    public IActionResult Warehouses() => View();

    [HttpPost]
    public Task<IActionResult> Warehouses(DataSourceRequest command) => WarehouseListCore();

    [HttpGet]
    public Task<IActionResult> CreateWarehouse() => WarehouseCreateGetCore();

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreateWarehouse(WarehouseModel model, bool continueEditing) =>
        WarehouseCreatePostCore(model, continueEditing);

    [HttpGet]
    public Task<IActionResult> EditWarehouse(string id) => WarehouseEditGetCore(id);

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditWarehouse(WarehouseModel model, bool continueEditing) =>
        WarehouseEditPostCore(model, continueEditing);

    [HttpPost]
    public Task<IActionResult> DeleteWarehouse(string id) => WarehouseDeleteCore(id);
}
