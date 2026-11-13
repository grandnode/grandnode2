using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Permissions;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

/// <summary>
///     The return methods of the store chosen in the store scope, as the shipping providers and settings take it
/// </summary>
[Area(Constants.AreaAdmin)]
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ReturnShippingController(
    IReturnShippingService returnShippingService,
    ISettingService settingService,
    ICountryService countryService,
    IRequestTranslationService translationService,
    IAdminStoreService adminStoreService)
    : BaseReturnShippingController(returnShippingService, settingService, countryService, translationService)
{
    [HttpGet]
    public async Task<IActionResult> Providers() => await SettingsCore(await adminStoreService.GetActiveStore());

    [HttpPost]
    public async Task<IActionResult> Providers(DataSourceRequest command) =>
        await ProvidersCore(await adminStoreService.GetActiveStore());

    [HttpPost]
    public async Task<IActionResult> ProviderUpdate(ReturnShippingProviderModel model) =>
        await ProviderUpdateCore(model, await adminStoreService.GetActiveStore());

    [HttpPost]
    public async Task<IActionResult> Settings(ReturnShippingSettingsModel model) =>
        await SettingsSaveCore(model, await adminStoreService.GetActiveStore());
}
