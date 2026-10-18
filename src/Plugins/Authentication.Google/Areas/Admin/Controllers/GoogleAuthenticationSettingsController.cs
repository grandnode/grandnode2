using Authentication.Google.Models;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Grand.Web.Common.Localization;

namespace Authentication.Google.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
public class GoogleAuthenticationSettingsController : BaseAdminPluginController
{
    #region Ctor

    public GoogleAuthenticationSettingsController(
        GoogleExternalAuthSettings googleExternalAuthSettings,
        IRequestTranslationService translationService,
        IRequestPermissionService permissionService,
        ISettingService settingService,
        IConfiguration configuration)
    {
        _googleExternalAuthSettings = googleExternalAuthSettings;
        _translationService = translationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _configuration = configuration;
    }

    #endregion

    #region Fields

    private readonly GoogleExternalAuthSettings _googleExternalAuthSettings;
    private readonly IRequestTranslationService _translationService;
    private readonly IRequestPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IConfiguration _configuration;

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
            return AccessDeniedView();

        var model = new ConfigurationModel {
            ClientKeyIdentifier = _configuration["GoogleSettings:ClientId"],
            ClientSecret = _configuration["GoogleSettings:ClientSecret"],
            DisplayOrder = _googleExternalAuthSettings.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        _googleExternalAuthSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(_googleExternalAuthSettings);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}