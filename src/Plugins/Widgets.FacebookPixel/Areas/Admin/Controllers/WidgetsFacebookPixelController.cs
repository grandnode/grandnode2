﻿using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Widgets.FacebookPixel.Models;

namespace Widgets.FacebookPixel.Areas.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Widgets)]
public class WidgetsFacebookPixelController : BaseAdminPluginController
{
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public WidgetsFacebookPixelController(IWorkContextAccessor workContextAccessor,
        IStoreService storeService,
        ISettingService settingService,
        ITranslationService translationService)
    {
        _workContextAccessor = workContextAccessor;
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

    [AuthorizeAdmin]
    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContextAccessor.WorkContext);
        var facebookPixelSettings = _settingService.LoadSetting<FacebookPixelSettings>(storeScope);
        var model = new ConfigurationModel {
            PixelId = facebookPixelSettings.PixelId,
            PixelScript = facebookPixelSettings.PixelScript,
            AddToCartScript = facebookPixelSettings.AddToCartScript,
            DetailsOrderScript = facebookPixelSettings.DetailsOrderScript,
            AllowToDisableConsentCookie = facebookPixelSettings.AllowToDisableConsentCookie,
            ConsentName = facebookPixelSettings.ConsentName,
            ConsentDescription = facebookPixelSettings.ConsentDescription,
            ConsentDefaultState = facebookPixelSettings.ConsentDefaultState
        };

        return View(model);
    }

    [HttpPost]
    [AuthorizeAdmin]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        //load settings for a chosen store scope
        var storeScope = await GetActiveStore(_storeService, _workContextAccessor.WorkContext);
        var facebookPixelSettings = _settingService.LoadSetting<FacebookPixelSettings>(storeScope);
        facebookPixelSettings.PixelId = model.PixelId;
        facebookPixelSettings.PixelScript = model.PixelScript;
        facebookPixelSettings.AddToCartScript = model.AddToCartScript;
        facebookPixelSettings.DetailsOrderScript = model.DetailsOrderScript;
        facebookPixelSettings.AllowToDisableConsentCookie = model.AllowToDisableConsentCookie;
        facebookPixelSettings.ConsentName = model.ConsentName;
        facebookPixelSettings.ConsentDescription = model.ConsentDescription;
        facebookPixelSettings.ConsentDefaultState = model.ConsentDefaultState;

        await _settingService.SaveSetting(facebookPixelSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();
        Success(_translationService.GetResource("Admin.Plugins.Saved"));
        return await Configure();
    }
}