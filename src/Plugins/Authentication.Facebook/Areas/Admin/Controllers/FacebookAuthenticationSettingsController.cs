using Authentication.Facebook.Models;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Authentication.Facebook.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
    public class FacebookAuthenticationSettingsController : BasePluginController
    {
        #region Fields

        private readonly FacebookExternalAuthSettings _facebookExternalAuthSettings;
        private readonly ITranslationService _translationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IConfiguration _configuration;

        #endregion

        #region Ctor

        public FacebookAuthenticationSettingsController(FacebookExternalAuthSettings facebookExternalAuthSettings,
            ITranslationService translationService,
            IPermissionService permissionService,
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
}