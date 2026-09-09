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
public class DeliveryDateController(
    IDeliveryDateService deliveryDateService,
    ILanguageService languageService,
    IStoreService storeService,
    ITranslationService translationService,
    IAdminDataScope<Grand.Domain.Shipping.DeliveryDate> scope)
    : BaseDeliveryDateController(deliveryDateService, languageService, storeService, translationService, scope)
{
    [HttpGet]
    public IActionResult DeliveryDates() => View();

    [HttpPost]
    public Task<IActionResult> DeliveryDates(DataSourceRequest command) => DeliveryDateListCore();

    [HttpGet]
    public Task<IActionResult> CreateDeliveryDate() => DeliveryDateCreateGetCore();

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> CreateDeliveryDate(DeliveryDateModel model, bool continueEditing) =>
        DeliveryDateCreatePostCore(model, continueEditing);

    [HttpGet]
    public Task<IActionResult> EditDeliveryDate(string id) => DeliveryDateEditGetCore(id);

    [HttpPost]
    [ArgumentNameFilter(KeyName = "save-continue", Argument = "continueEditing")]
    public Task<IActionResult> EditDeliveryDate(DeliveryDateModel model, bool continueEditing) =>
        DeliveryDateEditPostCore(model, continueEditing);

    [HttpPost]
    public Task<IActionResult> DeleteDeliveryDate(string id) => DeliveryDateDeleteCore(id);
}
