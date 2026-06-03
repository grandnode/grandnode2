using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Domain.Stores;
using Grand.Infrastructure.Caching;
using Grand.Web.AdminShared.Models.Settings;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Settings)]
public class BrandingController : BaseAdminController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly ICacheBase _cacheBase;

    public BrandingController(ISettingService settingService, ITranslationService translationService, ICacheBase cacheBase)
    {
        _settingService = settingService;
        _translationService = translationService;
        _cacheBase = cacheBase;
    }

    protected async Task ClearCache()
    {
        await _cacheBase.Clear();
    }

    public async Task<IActionResult> Index()
    {
        var storeScope = await GetActiveStore();
        var settings = await _settingService.LoadSetting<BrandingSettings>(storeScope);

        var model = new BrandingSettingsModel {
            ActiveStore = storeScope,
            PrimaryColor = settings.PrimaryColor,
            SecondaryColor = settings.SecondaryColor,
            AccentColor = settings.AccentColor,
            BackgroundColor = settings.BackgroundColor,
            TextColor = settings.TextColor,
            LogoPictureId = settings.LogoPictureId,
            FaviconPictureId = settings.FaviconPictureId,
            BannerPictureId = settings.BannerPictureId
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(BrandingSettingsModel model)
    {
        var storeScope = await GetActiveStore();
        var settings = await _settingService.LoadSetting<BrandingSettings>(storeScope);

        settings.PrimaryColor = SanitizeColor(model.PrimaryColor);
        settings.SecondaryColor = SanitizeColor(model.SecondaryColor);
        settings.AccentColor = SanitizeColor(model.AccentColor);
        settings.BackgroundColor = SanitizeColor(model.BackgroundColor);
        settings.TextColor = SanitizeColor(model.TextColor);
        settings.LogoPictureId = model.LogoPictureId;
        settings.FaviconPictureId = model.FaviconPictureId;
        settings.BannerPictureId = model.BannerPictureId;

        await _settingService.SaveSetting(settings, storeScope);
        await ClearCache();

        Success(_translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Index");
    }

    private static string SanitizeColor(string value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^#[0-9a-fA-F]{3,8}$") ? value : null;
    }
}
