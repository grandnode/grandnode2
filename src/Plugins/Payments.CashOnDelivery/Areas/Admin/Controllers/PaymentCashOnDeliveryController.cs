using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentCashOnDeliveryController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IAdminStoreService _adminStoreService;

    public PaymentCashOnDeliveryController(
        ISettingService settingService,
        ITranslationService translationService,
        IAdminStoreService adminStoreService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _adminStoreService = adminStoreService;
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var cashOnDeliveryPaymentSettings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText,
            AdditionalFee = cashOnDeliveryPaymentSettings.AdditionalFee,
            AdditionalFeePercentage = cashOnDeliveryPaymentSettings.AdditionalFeePercentage,
            ShippableProductRequired = cashOnDeliveryPaymentSettings.ShippableProductRequired,
            DisplayOrder = cashOnDeliveryPaymentSettings.DisplayOrder,
            SkipPaymentInfo = cashOnDeliveryPaymentSettings.SkipPaymentInfo
        };
        model.DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText;

        model.ActiveStore = storeScope;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var cashOnDeliveryPaymentSettings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(storeScope);

        //save settings
        cashOnDeliveryPaymentSettings.DescriptionText = model.DescriptionText;
        cashOnDeliveryPaymentSettings.AdditionalFee = model.AdditionalFee;
        cashOnDeliveryPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        cashOnDeliveryPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
        cashOnDeliveryPaymentSettings.DisplayOrder = model.DisplayOrder;
        cashOnDeliveryPaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

        await _settingService.SaveSetting(cashOnDeliveryPaymentSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}