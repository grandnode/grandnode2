using Authentication.Facebook.Models;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Grand.Web.Common.Localization;

namespace Authentication.Facebook.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
public class FacebookAuthenticationSettingsController : BasePluginController
{
    #region Ctor

    public FacebookAuthenticationSettingsController(FacebookExternalAuthSettings facebookExternalAuthSettings,
        IRequestTranslationService translationService,
        IRequestPermissionService permissionService,
        ISettingService settingService,
        IConfiguration configuration)
    {
        _facebookExternalAuthSettings = facebookExternalAuthSettings;
        _translationService = translationService;
        _permissionService = permissionService;
        _settingService = settingService;
        _configuration = configuration;
    }

    #endregion

    #region Fields

    private readonly FacebookExternalAuthSettings _facebookExternalAuthSettings;
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
            ClientId = _configuration["FacebookSettings:AppId"],
            ClientSecret = _configuration["FacebookSettings:AppSecret"],
            DisplayOrder = _facebookExternalAuthSettings.DisplayOrder
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

        _facebookExternalAuthSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(_facebookExternalAuthSettings);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}