using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.StripeCheckout.Models;

namespace Payments.StripeCheckout.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class StripeCheckoutController : BasePaymentController
{
    private readonly IPermissionService _permissionService;
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public StripeCheckoutController(IWorkContext workContext,
        IStoreService storeService,
        ISettingService settingService,
        ITranslationService translationService,
        IPermissionService permissionService)
    {
        _workContext = workContext;
        _storeService = storeService;
        _settingService = settingService;
        _translationService = translationService;
        _permissionService = permissionService;
    }

    private async Task<string> GetActiveStore()
    {
        var stores = await _storeService.GetAllStores();
        if (stores.Count < 2)
            return stores.FirstOrDefault()?.Id;

        var storeId =
            _workContext.CurrentCustomer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames
                .AdminAreaStoreScopeConfiguration);
        var store = await _storeService.GetStoreById(storeId);

        return store != null ? store.Id : "";
    }

    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.Authorize(StandardPermission.ManagePaymentMethods))
            return AccessDeniedView();

        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var stripeCheckoutPaymentSettings = _settingService.LoadSetting<StripeCheckoutPaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            ApiKey = stripeCheckoutPaymentSettings.ApiKey,
            WebhookEndpointSecret = stripeCheckoutPaymentSettings.WebhookEndpointSecret,
            Description = stripeCheckoutPaymentSettings.Description,
            Line = stripeCheckoutPaymentSettings.Line,
            DisplayOrder = stripeCheckoutPaymentSettings.DisplayOrder,
            StoreScope = storeScope
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await GetActiveStore();
        var stripeCheckoutPaymentSettings = _settingService.LoadSetting<StripeCheckoutPaymentSettings>(storeScope);

        //save settings
        stripeCheckoutPaymentSettings.ApiKey = model.ApiKey;
        stripeCheckoutPaymentSettings.WebhookEndpointSecret = model.WebhookEndpointSecret;
        stripeCheckoutPaymentSettings.Description = model.Description;
        stripeCheckoutPaymentSettings.Line = model.Line;
        stripeCheckoutPaymentSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(stripeCheckoutPaymentSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}