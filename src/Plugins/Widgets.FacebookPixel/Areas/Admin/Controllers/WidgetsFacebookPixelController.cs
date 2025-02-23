using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.FacebookPixel.Models;

namespace Widgets.FacebookPixel.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Widgets)]
public class WidgetsFacebookPixelController : BaseAdminPluginController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IAdminStoreService _adminStoreService;
    public WidgetsFacebookPixelController(
        ISettingService settingService,
        ITranslationService translationService,
        IAdminStoreService adminStoreService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _adminStoreService = adminStoreService;
    }

    [AuthorizeAdmin]
    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var facebookPixelSettings = await _settingService.LoadSetting<FacebookPixelSettings>(storeScope);
        var model = new ConfigurationModel {
            PixelId = facebookPixelSettings.PixelId,
            PixelScript = facebookPixelSettings.PixelScript,
            AddToCartScript = facebookPixelSettings.AddToCartScript,
            DetailsOrderScript = facebookPixelSettings.DetailsOrderScript,
            AllowToDisableConsentCookie = facebookPixelSettings.AllowToDisableConsentCookie,
            ConsentName = facebookPixelSettings.ConsentName,
            ConsentDescription = facebookPixelSettings.ConsentDescription,
            ConsentDefaultState = facebookPixelSettings.ConsentDefaultState
        };

        return View(model);
    }

    [HttpPost]
    [AuthorizeAdmin]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var facebookPixelSettings = await _settingService.LoadSetting<FacebookPixelSettings>(storeScope);
        facebookPixelSettings.PixelId = model.PixelId;
        facebookPixelSettings.PixelScript = model.PixelScript;
        facebookPixelSettings.AddToCartScript = model.AddToCartScript;
        facebookPixelSettings.DetailsOrderScript = model.DetailsOrderScript;
        facebookPixelSettings.AllowToDisableConsentCookie = model.AllowToDisableConsentCookie;
        facebookPixelSettings.ConsentName = model.ConsentName;
        facebookPixelSettings.ConsentDescription = model.ConsentDescription;
        facebookPixelSettings.ConsentDefaultState = model.ConsentDefaultState;

        await _settingService.SaveSetting(facebookPixelSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();
        Success(_translationService.GetResource("Admin.Plugins.Saved"));
        return await Configure();
    }
}