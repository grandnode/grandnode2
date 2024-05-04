using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.GoogleAnalytics.Models;

namespace Widgets.GoogleAnalytics.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Widgets)]
public class WidgetsGoogleAnalyticsController : BaseAdminPluginController
{
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public WidgetsGoogleAnalyticsController(IWorkContext workContext,
        IStoreService storeService,
        ISettingService settingService,
        ITranslationService translationService)
    {
        _workContext = workContext;
        _storeService = storeService;
        _settingService = settingService;
        _translationService = translationService;
    }

    protected virtual async Task<string> GetActiveStore(IStoreService storeService, IWorkContext workContext)
    {
        var stores = await storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault()!.Id;

        var storeId =
            workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        var store = await storeService.GetStoreById(storeId);

        return store != null ? store.Id : "";
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContext);
        var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsEcommerceSettings>(storeScope);
        var model = new ConfigurationModel {
            GoogleId = googleAnalyticsSettings.GoogleId,
            TrackingScript = googleAnalyticsSettings.TrackingScript,
            EcommerceScript = googleAnalyticsSettings.EcommerceScript,
            EcommerceDetailScript = googleAnalyticsSettings.EcommerceDetailScript,
            IncludingTax = googleAnalyticsSettings.IncludingTax,
            AllowToDisableConsentCookie = googleAnalyticsSettings.AllowToDisableConsentCookie,
            ConsentDefaultState = googleAnalyticsSettings.ConsentDefaultState,
            ConsentName = googleAnalyticsSettings.ConsentName,
            ConsentDescription = googleAnalyticsSettings.ConsentDescription,
            StoreScope = storeScope
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContext);
        var googleAnalyticsSettings = _settingService.LoadSetting<GoogleAnalyticsEcommerceSettings>(storeScope);
        googleAnalyticsSettings.GoogleId = model.GoogleId;
        googleAnalyticsSettings.TrackingScript = model.TrackingScript;
        googleAnalyticsSettings.EcommerceScript = model.EcommerceScript;
        googleAnalyticsSettings.EcommerceDetailScript = model.EcommerceDetailScript;
        googleAnalyticsSettings.IncludingTax = model.IncludingTax;
        googleAnalyticsSettings.AllowToDisableConsentCookie = model.AllowToDisableConsentCookie;
        googleAnalyticsSettings.ConsentDefaultState = model.ConsentDefaultState;
        googleAnalyticsSettings.ConsentName = model.ConsentName;
        googleAnalyticsSettings.ConsentDescription = model.ConsentDescription;

        await _settingService.SaveSetting(googleAnalyticsSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}