using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the cash on delivery payment method.
///     <para>
///         The settings are read and written for the store the manager is bound to
///         (<see cref="CurrentStoreId" />), never globally - the storefront already resolves every
///         <c>ISettings</c> class for the current store, so a per-store row takes effect on checkout
///         of that store only. Note that the very first save creates a store row covering the whole
///         settings class, after which this store stops inheriting later global changes to it.
///     </para>
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentCashOnDeliveryController : BasePaymentController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;

    public PaymentCashOnDeliveryController(
        ISettingService settingService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _settingService = settingService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current store manager is bound to. AuthorizeStore already rejects a
    ///     customer without one, so this is never empty here.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public async Task<IActionResult> Configure()
    {
        var cashOnDeliveryPaymentSettings =
            await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(CurrentStoreId);

        var model = new ConfigurationModel {
            DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText,
            AdditionalFee = cashOnDeliveryPaymentSettings.AdditionalFee,
            AdditionalFeePercentage = cashOnDeliveryPaymentSettings.AdditionalFeePercentage,
            ShippableProductRequired = cashOnDeliveryPaymentSettings.ShippableProductRequired,
            DisplayOrder = cashOnDeliveryPaymentSettings.DisplayOrder,
            SkipPaymentInfo = cashOnDeliveryPaymentSettings.SkipPaymentInfo,
            //the store manager can only configure his own store, so no store selector is offered
            ActiveStore = CurrentStoreId
        };

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var cashOnDeliveryPaymentSettings =
            await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(CurrentStoreId);

        cashOnDeliveryPaymentSettings.DescriptionText = model.DescriptionText;
        cashOnDeliveryPaymentSettings.AdditionalFee = model.AdditionalFee;
        cashOnDeliveryPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        cashOnDeliveryPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
        cashOnDeliveryPaymentSettings.DisplayOrder = model.DisplayOrder;
        cashOnDeliveryPaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

        await _settingService.SaveSetting(cashOnDeliveryPaymentSettings, CurrentStoreId);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}
