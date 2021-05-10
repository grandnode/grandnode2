using Grand.Business.Authentication.Extensions;
using Grand.Business.Authentication.Interfaces;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Services.Security;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Grand.Domain.Customers;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.ExternalAuthentication;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
    public partial class ExternalAuthenticationController : BaseAdminController
    {
        #region Fields

        private readonly IExternalAuthenticationService _openAuthenticationService;
        private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
        private readonly ISettingService _settingService;
        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructors

        public ExternalAuthenticationController(IExternalAuthenticationService openAuthenticationService,
            ExternalAuthenticationSettings externalAuthenticationSettings,
            ISettingService settingService,
            IServiceProvider serviceProvider)
        {
            _openAuthenticationService = openAuthenticationService;
            _externalAuthenticationSettings = externalAuthenticationSettings;
            _settingService = settingService;
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Methods

        public IActionResult Methods() => View();

        [HttpPost]
        public IActionResult Methods(DataSourceRequest command)
        {
            var methodsModel = new List<AuthenticationMethodModel>();
            var methods = _openAuthenticationService.LoadAllAuthenticationProviders();
            foreach (var method in methods)
            {
                var tmp = method.ToModel();
                tmp.IsActive = method.IsMethodActive(_externalAuthenticationSettings);
                var url = method.ConfigurationUrl;

                if (string.IsNullOrEmpty(url))
                    url = PluginManager.ReferencedPlugins.FirstOrDefault(x => x.SystemName.Equals(method.SystemName, StringComparison.OrdinalIgnoreCase))
                        ?.Instance<IPlugin>(_serviceProvider)?.ConfigurationUrl();
                tmp.ConfigurationUrl = url;
                methodsModel.Add(tmp);
            }
            methodsModel = methodsModel.ToList();
            var gridModel = new DataSourceResult
            {
                Data = methodsModel,
                Total = methodsModel.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> MethodUpdate(AuthenticationMethodModel model)
        {
            var eam = _openAuthenticationService.LoadAuthenticationProviderBySystemName(model.SystemName);
            if (eam.IsMethodActive(_externalAuthenticationSettings))
            {
                if (!model.IsActive)
                {
                    //mark as disabled
                    _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Remove(eam.SystemName);
                    await _settingService.SaveSetting(_externalAuthenticationSettings);
                }
            }
            else
            {
                if (model.IsActive)
                {
                    //mark as active
                    _externalAuthenticationSettings.ActiveAuthenticationMethodSystemNames.Add(eam.SystemName);
                    await _settingService.SaveSetting(_externalAuthenticationSettings);
                }
            }

            return new JsonResult("");
        }

        #endregion
    }
}