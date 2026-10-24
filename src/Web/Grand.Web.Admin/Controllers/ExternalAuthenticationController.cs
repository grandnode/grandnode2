using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Permissions;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Models.ExternalAuthentication;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
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

    /// <summary>SCIM provisioning: the endpoint comes from appsettings and is shown read-only (never the token)</summary>
    public async Task<IActionResult> Scim([FromServices] ScimConfig scimConfig, [FromServices] OidcLoginConfig loginConfig,
        [FromServices] OidcResourceServerConfig resourceServerConfig, [FromServices] IConfiguration configuration,
        [FromServices] IScimUserService scimUserService, [FromServices] ICustomerService customerService)
    {
        var conflictIds = await scimUserService.GetConflicts();
        var conflicts = conflictIds.Count == 0 ? [] : await customerService.GetCustomersByIds(conflictIds.ToArray());
        return View(new ScimModel {
            ModuleEnabled = configuration.GetValue<bool>("FeatureManagement:Grand.Module.Scim"),
            TokenUsable = scimConfig.TokenUsable,
            SignInConfigured = loginConfig.IsConfigured(resourceServerConfig),
            EndpointUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/scim/v2",
            ManagedAccounts = await scimUserService.Count(),
            Conflicts = conflicts.Select(x => new ScimConflictModel { CustomerId = x.Id, Email = x.Email }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> ScimGroups(DataSourceRequest command, [FromServices] IScimGroupService scimGroupService,
        [FromServices] IStoreService storeService, [FromServices] IVendorService vendorService)
    {
        var groups = (await scimGroupService.GetMappableGroups()).ToDictionary(x => x.Id, x => x.Name);
        var stores = (await storeService.GetAllStores()).ToDictionary(x => x.Id, x => x.Shortcut);
        var vendors = (await vendorService.GetAllVendors(showHidden: true)).ToDictionary(x => x.Id, x => x.Name);
        var rows = (await scimGroupService.GetMappings()).Select(x => new ScimGroupRowModel {
            Id = x.GroupId,
            DisplayName = x.DisplayName,
            MemberCount = x.MemberCount,
            CustomerGroup = groups.GetValueOrDefault(x.CustomerGroupId ?? ""),
            Store = stores.GetValueOrDefault(x.StaffStoreId ?? ""),
            Vendor = vendors.GetValueOrDefault(x.VendorId ?? "")
        }).ToList();

        return Json(new DataSourceResult {
            Data = rows.Skip((command.Page - 1) * command.PageSize).Take(command.PageSize),
            Total = rows.Count
        });
    }

    public async Task<IActionResult> ScimGroupEdit(string id, [FromServices] IScimGroupService scimGroupService,
        [FromServices] IStoreService storeService, [FromServices] IVendorService vendorService)
    {
        var group = await scimGroupService.Get(id);
        if (group.Outcome != ScimOutcome.Ok)
            return RedirectToAction("Scim");

        var mapping = (await scimGroupService.GetMappings()).FirstOrDefault(x => x.GroupId == id);
        var model = new ScimGroupMappingModel {
            Id = group.Group.Id,
            CustomerGroupId = mapping?.CustomerGroupId,
            StaffStoreId = mapping?.StaffStoreId,
            VendorId = mapping?.VendorId
        };
        await PrepareScimGroupMapping(model, group.Group, scimGroupService, storeService, vendorService);
        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> ScimGroupEdit(ScimGroupMappingModel model,
        [FromServices] IScimGroupService scimGroupService, [FromServices] IStoreService storeService,
        [FromServices] IVendorService vendorService, [FromServices] IRequestTranslationService translationService)
    {
        var outcome = await scimGroupService.SetMapping(model.Id, model.CustomerGroupId, model.StaffStoreId, model.VendorId);
        if (outcome == ScimOutcome.Ok)
        {
            Success(translationService.GetResource("Admin.Plugins.ExternalAuthenticationMethods.Scim.Mapping.Saved"));
            return RedirectToAction("Scim");
        }

        var group = await scimGroupService.Get(model.Id);
        if (outcome == ScimOutcome.NotFound || group.Outcome != ScimOutcome.Ok)
            return RedirectToAction("Scim");

        ModelState.AddModelError("",
            translationService.GetResource("Admin.Plugins.ExternalAuthenticationMethods.Scim.Mapping.Invalid"));
        await PrepareScimGroupMapping(model, group.Group, scimGroupService, storeService, vendorService);
        return View(model);
    }

    private static async Task PrepareScimGroupMapping(ScimGroupMappingModel model, ScimGroupView group,
        IScimGroupService scimGroupService, IStoreService storeService, IVendorService vendorService)
    {
        model.DisplayName = group.DisplayName;
        model.MemberCount = group.MemberIds.Count;
        model.AvailableCustomerGroups = (await scimGroupService.GetMappableGroups())
            .Select(x => new SelectListItem(x.Name, x.Id)).ToList();
        model.AvailableStores = (await storeService.GetAllStores())
            .Select(x => new SelectListItem(x.Shortcut, x.Id)).ToList();
        model.AvailableVendors = (await vendorService.GetAllVendors(showHidden: true))
            .Select(x => new SelectListItem(x.Name, x.Id)).ToList();
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