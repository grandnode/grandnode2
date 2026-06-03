using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Stores;
using Grand.Web.AdminShared.Models.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

public class BrandingController : BaseAdminController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;

    public BrandingController(ISettingService settingService, ITranslationService translationService)
    {
        _settingService = settingService;
        _translationService = translationService;
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

        settings.PrimaryColor = model.PrimaryColor;
        settings.SecondaryColor = model.SecondaryColor;
        settings.AccentColor = model.AccentColor;
        settings.BackgroundColor = model.BackgroundColor;
        settings.TextColor = model.TextColor;
        settings.LogoPictureId = model.LogoPictureId;
        settings.FaviconPictureId = model.FaviconPictureId;
        settings.BannerPictureId = model.BannerPictureId;

        await _settingService.SaveSetting(settings, storeScope);

        Success(_translationService.GetResource("Admin.Configuration.Updated"));
        return RedirectToAction("Index");
    }
}
