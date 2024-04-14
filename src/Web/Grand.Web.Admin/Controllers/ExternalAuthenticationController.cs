using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Customers;
using Grand.Infrastructure.Plugins;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.ExternalAuthentication;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.ExternalAuthenticationMethods)]
public class ExternalAuthenticationController : BaseAdminController
{
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

    #region Fields

    private readonly IExternalAuthenticationService _openAuthenticationService;
    private readonly ExternalAuthenticationSettings _externalAuthenticationSettings;
    private readonly ISettingService _settingService;
    private readonly IServiceProvider _serviceProvider;

    #endregion

    #region Methods

    public IActionResult Methods()
    {
        return View();
    }

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
                url = PluginManager.ReferencedPlugins.FirstOrDefault(x =>
                        x.SystemName.Equals(method.SystemName, StringComparison.OrdinalIgnoreCase))
                    ?.Instance<IPlugin>(_serviceProvider)?.ConfigurationUrl();
            tmp.ConfigurationUrl = url;
            methodsModel.Add(tmp);
        }

        methodsModel = methodsModel.ToList();
        var gridModel = new DataSourceResult {
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