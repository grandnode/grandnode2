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
[Route("[area]/Shipping/[action]/{id?}")]
public class ShippingMethodController(
    IShippingMethodService shippingMethodService,
    ILanguageService languageService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.ShippingMethod> scope)
    : BaseShippingMethodController(shippingMethodService, languageService, storeService, translationService, scope)
{
    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public IActionResult Methods() => View();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> MethodsListData() => ShippingMethodListCore();

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    public Task<IActionResult> CreateMethod() => ShippingMethodCreateGetCore();

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Create)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreateMethod(ShippingMethodModel model, bool continueEditing) =>
        ShippingMethodCreatePostCore(model, continueEditing);

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> EditMethod(string id) => ShippingMethodEditGetCore(id);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditMethod(ShippingMethodModel model, bool continueEditing) =>
        ShippingMethodEditPostCore(model, continueEditing);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public Task<IActionResult> DeleteMethod(string id) => ShippingMethodDeleteCore(id);
}
