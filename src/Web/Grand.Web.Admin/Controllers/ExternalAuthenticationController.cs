using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Permissions;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Models.ExternalAuthentication;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

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

    public async Task<IActionResult> Methods([FromServices] OidcResourceServerConfig resourceServerConfig,
        [FromServices] OidcLoginConfig loginConfig, [FromServices] IOidcLinkService linkService)
    {
        //the sign-in card follows the sign-in method (Authority and ClientId), the links card the resource server
        var signInConfigured = loginConfig.IsConfigured(resourceServerConfig);
        var linksConfigured = ResourceServerConfigured(resourceServerConfig);
        if (!signInConfigured && !linksConfigured)
            return View();

        var model = new OidcLinksModel {
            SignInConfigured = signInConfigured,
            LinksConfigured = linksConfigured,
            ConfiguredAuthority = resourceServerConfig.Authority,
            ClientId = loginConfig.ClientId,
            ClientSecretSet = !string.IsNullOrWhiteSpace(loginConfig.ClientSecret)
        };
        if (linksConfigured)
        {
            var state = await linkService.GetState();
            model.ConfiguredAuthority = state.ConfiguredAuthority;
            model.LinkedAuthority = state.LinkedAuthority;
            model.LinkCount = state.LinkCount;
            model.ProviderChanged = state.ProviderChanged;
            model.ProviderUnknown = state.ProviderUnknown;
        }

        return View(model);
    }

    /// <summary>Another identity provider: every OpenID Connect link is removed, people connect again at their next sign-in</summary>
    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> RemoveOidcLinks([FromServices] IOidcLinkService linkService,
        [FromServices] IRequestTranslationService translationService, [FromServices] OidcResourceServerConfig resourceServerConfig)
    {
        //the links are recorded for the configured provider: without one there is nothing to record them for
        if (!ResourceServerConfigured(resourceServerConfig))
            return RedirectToAction("Methods");

        var count = await linkService.RemoveAll();
        Success(string.Format(translationService.GetResource("Admin.Plugins.ExternalAuthenticationMethods.OidcLinks.Removed"),
            count));
        return RedirectToAction("Methods");
    }

    /// <summary>The same identity provider at a new address: the links are kept for it</summary>
    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> KeepOidcLinks([FromServices] IOidcLinkService linkService,
        [FromServices] IRequestTranslationService translationService, [FromServices] OidcResourceServerConfig resourceServerConfig)
    {
        if (!ResourceServerConfigured(resourceServerConfig))
            return RedirectToAction("Methods");

        await linkService.KeepAll();
        Success(translationService.GetResource("Admin.Plugins.ExternalAuthenticationMethods.OidcLinks.Kept"));
        return RedirectToAction("Methods");
    }

    private static bool ResourceServerConfigured(OidcResourceServerConfig config) =>
        config.Enabled && !string.IsNullOrWhiteSpace(config.Authority);

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