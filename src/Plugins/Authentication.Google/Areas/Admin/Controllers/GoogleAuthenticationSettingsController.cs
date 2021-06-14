using Authentication.Google.Models;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Authentication.Google.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
    public class GoogleAuthenticationSettingsController : BasePluginController
    {
        #region Fields

        private readonly GoogleExternalAuthSettings _googleExternalAuthSettings;
        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;

        #endregion

        #region Ctor

        public GoogleAuthenticationSettingsController(
            GoogleExternalAuthSettings googleExternalAuthSettings,
            ITranslationService translationService,
            IPermissionService permissionService,
            ISettingService settingService)
        {
            _googleExternalAuthSettings = googleExternalAuthSettings;
            _translationService = translationService;
            _permissionService = permissionService;
            _settingService = settingService;
        }

        #endregion

        #region Methods


        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel {
                ClientKeyIdentifier = _googleExternalAuthSettings.ClientKeyIdentifier,
                ClientSecret = _googleExternalAuthSettings.ClientSecret,
                DisplayOrder = _googleExternalAuthSettings.DisplayOrder
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageExternalAuthenticationMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            _googleExternalAuthSettings.ClientKeyIdentifier = model.ClientKeyIdentifier;
            _googleExternalAuthSettings.ClientSecret = model.ClientSecret;
            _googleExternalAuthSettings.DisplayOrder = model.DisplayOrder;

            await _settingService.SaveSetting(_googleExternalAuthSettings);

            //now clear settings cache
            await _settingService.ClearCache();

            Success(_translationService.GetResource("Admin.Plugins.Saved"));

            return await Configure();

        }

        #endregion
    }
}