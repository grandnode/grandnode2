using Grand.Business.Core.Interfaces.Checkout.Shipping;
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
public class DeliveryDateController(
    IDeliveryDateService deliveryDateService,
    ILanguageService languageService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.DeliveryDate> scope)
    : BaseDeliveryDateController(deliveryDateService, languageService, storeService, translationService, scope)
{
    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult DeliveryDates() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> DeliveryDatesListData() => DeliveryDateListCore();

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> CreateDeliveryDate() => DeliveryDateCreateGetCore();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreateDeliveryDate(DeliveryDateModel model, bool continueEditing) =>
        DeliveryDateCreatePostCore(model, continueEditing);

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> EditDeliveryDate(string id) => DeliveryDateEditGetCore(id);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditDeliveryDate(DeliveryDateModel model, bool continueEditing) =>
        DeliveryDateEditPostCore(model, continueEditing);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public Task<IActionResult> DeleteDeliveryDate(string id) => DeliveryDateDeleteCore(id);
}
