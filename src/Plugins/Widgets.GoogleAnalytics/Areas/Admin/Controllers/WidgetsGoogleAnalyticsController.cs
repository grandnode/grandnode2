using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.GoogleAnalytics.Models;

namespace Widgets.GoogleAnalytics.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Widgets)]
public class WidgetsGoogleAnalyticsController : BaseAdminPluginController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IAdminStoreService _adminStoreService;

    public WidgetsGoogleAnalyticsController(
        ISettingService settingService,
        ITranslationService translationService,
        IAdminStoreService adminStoreService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _adminStoreService = adminStoreService;
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var googleAnalyticsSettings = await _settingService.LoadSetting<GoogleAnalyticsEcommerceSettings>(storeScope);
        var model = new ConfigurationModel {
            GoogleId = googleAnalyticsSettings.GoogleId,
            TrackingScript = googleAnalyticsSettings.TrackingScript,
            EcommerceScript = googleAnalyticsSettings.EcommerceScript,
            EcommerceDetailScript = googleAnalyticsSettings.EcommerceDetailScript,
            IncludingTax = googleAnalyticsSettings.IncludingTax,
            AllowToDisableConsentCookie = googleAnalyticsSettings.AllowToDisableConsentCookie,
            ConsentDefaultState = googleAnalyticsSettings.ConsentDefaultState,
            ConsentName = googleAnalyticsSettings.ConsentName,
            ConsentDescription = googleAnalyticsSettings.ConsentDescription,
            StoreScope = storeScope
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var googleAnalyticsSettings = await _settingService.LoadSetting<GoogleAnalyticsEcommerceSettings>(storeScope);
        googleAnalyticsSettings.GoogleId = model.GoogleId;
        googleAnalyticsSettings.TrackingScript = model.TrackingScript;
        googleAnalyticsSettings.EcommerceScript = model.EcommerceScript;
        googleAnalyticsSettings.EcommerceDetailScript = model.EcommerceDetailScript;
        googleAnalyticsSettings.IncludingTax = model.IncludingTax;
        googleAnalyticsSettings.AllowToDisableConsentCookie = model.AllowToDisableConsentCookie;
        googleAnalyticsSettings.ConsentDefaultState = model.ConsentDefaultState;
        googleAnalyticsSettings.ConsentName = model.ConsentName;
        googleAnalyticsSettings.ConsentDescription = model.ConsentDescription;

        await _settingService.SaveSetting(googleAnalyticsSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}