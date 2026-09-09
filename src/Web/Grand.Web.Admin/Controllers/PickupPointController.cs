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
    public IActionResult PickupPoints() => View();

    [HttpPost]
    public Task<IActionResult> PickupPoints(DataSourceRequest command) => PickupPointListCore();

    [HttpGet]
    public Task<IActionResult> CreatePickupPoint() => PickupPointCreateGetCore();

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreatePickupPoint(PickupPointModel model, bool continueEditing) =>
        PickupPointCreatePostCore(model, continueEditing);

    [HttpGet]
    public Task<IActionResult> EditPickupPoint(string id) => PickupPointEditGetCore(id);

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditPickupPoint(PickupPointModel model, bool continueEditing) =>
        PickupPointEditPostCore(model, continueEditing);

    [HttpPost]
    public Task<IActionResult> DeletePickupPoint(string id) => PickupPointDeleteCore(id);
}
