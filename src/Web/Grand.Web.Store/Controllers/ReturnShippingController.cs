using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

/// <summary>
///     The return methods of the store manager's own store
/// </summary>
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AutoValidateAntiforgeryToken]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.ShippingSettings)]
public class ReturnShippingController(
    IReturnShippingService returnShippingService,
    ISettingService settingService,
    ICountryService countryService,
    IRequestTranslationService translationService,
    IContextAccessor contextAccessor)
    : BaseReturnShippingController(returnShippingService, settingService, countryService, translationService)
{
    private string CurrentStoreId => contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    //a provider whose plugin is not multi-store has no Store-area configuration screen - its relative url would
    //resolve under /Store/ and 404, so offer no link at all
    protected override string ConfigurationUrl(IReturnShippingProvider provider) =>
        StoreAreaConfiguration.Exists(provider) ? provider.ConfigurationUrl : null;

    [HttpGet]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> Providers() => SettingsCore(CurrentStoreId);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public Task<IActionResult> Providers(DataSourceRequest command) => ProvidersCore(CurrentStoreId);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> ProviderUpdate(ReturnShippingProviderModel model) =>
        ProviderUpdateCore(model, CurrentStoreId);

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public Task<IActionResult> Settings(ReturnShippingSettingsModel model) => SettingsSaveCore(model, CurrentStoreId);
}
