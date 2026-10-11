using Authentication.OpenIdConnect.Models;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Infrastructure.Configuration;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authentication.OpenIdConnect.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
public class OidcLoginSettingsController : BaseAdminPluginController
{
    #region Ctor

    public OidcLoginSettingsController(
        OidcLoginSettings oidcLoginSettings,
        OidcResourceServerConfig resourceServerConfig,
        OidcLoginConfig oidcLoginConfig,
        ITranslationService translationService,
        IPermissionService permissionService,
        ISettingService settingService)
    {
        _oidcLoginSettings = oidcLoginSettings;
        _resourceServerConfig = resourceServerConfig;
        _oidcLoginConfig = oidcLoginConfig;
        _translationService = translationService;
        _permissionService = permissionService;
        _settingService = settingService;
    }

    #endregion

    #region Fields

    private readonly OidcLoginSettings _oidcLoginSettings;
    private readonly OidcResourceServerConfig _resourceServerConfig;
    private readonly OidcLoginConfig _oidcLoginConfig;
    private readonly ITranslationService _translationService;
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
            return AccessDeniedView();

        var model = new ConfigurationModel {
            Authority = _resourceServerConfig.Authority,
            ClientId = _oidcLoginConfig.ClientId,
            //never the secret itself
            ClientSecret = _translationService.GetResource(string.IsNullOrWhiteSpace(_oidcLoginConfig.ClientSecret)
                ? "Plugins.ExternalAuth.OpenIdConnect.ClientSecret.NotSet"
                : "Plugins.ExternalAuth.OpenIdConnect.ClientSecret.Set"),
            DisplayOrder = _oidcLoginSettings.DisplayOrder
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

        _oidcLoginSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(_oidcLoginSettings);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
