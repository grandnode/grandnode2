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
[Route("[area]/Shipping/[action]")]
public class PickupPointController(
    IPickupPointService pickupPointService,
    IWarehouseService warehouseService,
    ICountryService countryService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.PickupPoint> scope)
    : BasePickupPointController(pickupPointService, warehouseService, countryService, storeService, translationService, scope)
{
    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult PickupPoints() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> PickupPointsListData() => PickupPointListCore();

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> CreatePickupPoint() => PickupPointCreateGetCore();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreatePickupPoint(PickupPointModel model, bool continueEditing) =>
        PickupPointCreatePostCore(model, continueEditing);

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> EditPickupPoint(string id) => PickupPointEditGetCore(id);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditPickupPoint(PickupPointModel model, bool continueEditing) =>
        PickupPointEditPostCore(model, continueEditing);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public Task<IActionResult> DeletePickupPoint(string id) => PickupPointDeleteCore(id);
}
