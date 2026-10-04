using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.BrainTree.Models;

namespace Payments.BrainTree.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentBrainTreeController : BasePaymentController
{
    #region Ctor

    public PaymentBrainTreeController(ISettingService settingService,
        ITranslationService translationService,
        IAdminStoreService adminStoreService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _adminStoreService = adminStoreService;
    }

    #endregion

    #region Fields

    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IAdminStoreService _adminStoreService;

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope - the injected settings instance would always be the
        //one of the store hosting the admin panel, which ignores the scope the admin selected
        var storeScope = await _adminStoreService.GetActiveStore();
        var brainTreePaymentSettings = await _settingService.LoadSetting<BrainTreePaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            Use3DS = brainTreePaymentSettings.Use3DS,
            UseSandBox = brainTreePaymentSettings.UseSandBox,
            PublicKey = brainTreePaymentSettings.PublicKey,
            PrivateKey = brainTreePaymentSettings.PrivateKey,
            MerchantId = brainTreePaymentSettings.MerchantId,
            AdditionalFee = brainTreePaymentSettings.AdditionalFee,
            AdditionalFeePercentage = brainTreePaymentSettings.AdditionalFeePercentage,
            DisplayOrder = brainTreePaymentSettings.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var brainTreePaymentSettings = await _settingService.LoadSetting<BrainTreePaymentSettings>(storeScope);

        //save settings
        brainTreePaymentSettings.Use3DS = model.Use3DS;
        brainTreePaymentSettings.UseSandBox = model.UseSandBox;
        brainTreePaymentSettings.PublicKey = model.PublicKey;
        brainTreePaymentSettings.PrivateKey = model.PrivateKey;
        brainTreePaymentSettings.MerchantId = model.MerchantId;
        brainTreePaymentSettings.AdditionalFee = model.AdditionalFee;
        brainTreePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        brainTreePaymentSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(brainTreePaymentSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}