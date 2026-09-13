using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.BrainTree.Models;

namespace Payments.BrainTree.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the BrainTree payment method.
///     <para>
///         The settings - the gateway credentials included - are read and written for the store the
///         manager is bound to (<see cref="CurrentStoreId" />), so each store transacts on its own
///         BrainTree account. The provider builds a <c>BraintreeGateway</c> per call from the settings
///         instance the container resolved for the current store, so no further change is needed for
///         the payment itself to use them. Note that the first save creates a store row covering the
///         whole settings class, after which this store stops inheriting later global changes to it.
///     </para>
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentBrainTreeController : BasePaymentController
{
    #region Ctor

    public PaymentBrainTreeController(ISettingService settingService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _settingService = settingService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Fields

    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    /// <summary>
    ///     The store the current store manager is bound to. AuthorizeStore already rejects a
    ///     customer without one, so this is never empty here.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    #endregion

    #region Methods

    public async Task<IActionResult> Configure()
    {
        var brainTreePaymentSettings = await _settingService.LoadSetting<BrainTreePaymentSettings>(CurrentStoreId);

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

        var brainTreePaymentSettings = await _settingService.LoadSetting<BrainTreePaymentSettings>(CurrentStoreId);

        //save settings
        brainTreePaymentSettings.Use3DS = model.Use3DS;
        brainTreePaymentSettings.UseSandBox = model.UseSandBox;
        brainTreePaymentSettings.PublicKey = model.PublicKey;
        brainTreePaymentSettings.PrivateKey = model.PrivateKey;
        brainTreePaymentSettings.MerchantId = model.MerchantId;
        brainTreePaymentSettings.AdditionalFee = model.AdditionalFee;
        brainTreePaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        brainTreePaymentSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(brainTreePaymentSettings, CurrentStoreId);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion
}
