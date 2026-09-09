using Grand.Business.Core.Interfaces.Checkout.Shipping;
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
public class ShippingMethodController(
    IShippingMethodService shippingMethodService,
    ILanguageService languageService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.ShippingMethod> scope)
    : BaseShippingMethodController(shippingMethodService, languageService, storeService, translationService, scope)
{
    [HttpGet]
    public IActionResult Methods() => View();

    [HttpPost]
    public Task<IActionResult> Methods(DataSourceRequest command) => ShippingMethodListCore();

    [HttpGet]
    public Task<IActionResult> CreateMethod() => ShippingMethodCreateGetCore();

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreateMethod(ShippingMethodModel model, bool continueEditing) =>
        ShippingMethodCreatePostCore(model, continueEditing);

    [HttpGet]
    public Task<IActionResult> EditMethod(string id) => ShippingMethodEditGetCore(id);

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditMethod(ShippingMethodModel model, bool continueEditing) =>
        ShippingMethodEditPostCore(model, continueEditing);

    [HttpPost]
    public Task<IActionResult> DeleteMethod(string id) => ShippingMethodDeleteCore(id);
}
