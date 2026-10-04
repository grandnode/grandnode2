using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.StripeCheckout.Models;

namespace Payments.StripeCheckout.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of the Stripe Checkout payment method.
///     <para>
///         The settings - the API key and the webhook secret included - are read and written for the
///         store the manager is bound to (<see cref="CurrentStoreId" />), so each store transacts on
///         its own Stripe account. Every Stripe call is made through a client built from the settings
///         instance the container resolved for the current store. Note that the first save creates a
///         store row covering the whole settings class, after which this store stops inheriting later
///         global changes to it.
///     </para>
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class StripeCheckoutController : BasePaymentController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;

    public StripeCheckoutController(
        ISettingService settingService,
        ITranslationService translationService,
        IStoreService storeService,
        IContextAccessor contextAccessor)
    {
        _settingService = settingService;
        _translationService = translationService;
        _storeService = storeService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current store manager is bound to. AuthorizeStore already rejects a
    ///     customer without one, so this is never empty here.
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public async Task<IActionResult> Configure()
    {
        var stripeCheckoutPaymentSettings =
            await _settingService.LoadSetting<StripeCheckoutPaymentSettings>(CurrentStoreId);

        var model = new ConfigurationModel {
            ApiKey = stripeCheckoutPaymentSettings.ApiKey,
            WebhookEndpointSecret = stripeCheckoutPaymentSettings.WebhookEndpointSecret,
            Description = stripeCheckoutPaymentSettings.Description,
            Line = stripeCheckoutPaymentSettings.Line,
            DisplayOrder = stripeCheckoutPaymentSettings.DisplayOrder,
            //the store manager can only configure his own store, so no store selector is offered
            StoreScope = CurrentStoreId
        };

        //the webhook has to be registered with Stripe against the manager's own storefront, not
        //against whichever host happens to serve this panel
        var store = await _storeService.GetStoreById(CurrentStoreId);
        ViewBag.WebhookUrl = $"{store?.Url?.TrimEnd('/')}{Url.RouteUrl(StripeCheckoutDefaults.WebHook)}";

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var stripeCheckoutPaymentSettings =
            await _settingService.LoadSetting<StripeCheckoutPaymentSettings>(CurrentStoreId);

        stripeCheckoutPaymentSettings.ApiKey = model.ApiKey;
        stripeCheckoutPaymentSettings.WebhookEndpointSecret = model.WebhookEndpointSecret;
        stripeCheckoutPaymentSettings.Description = model.Description;
        stripeCheckoutPaymentSettings.Line = model.Line;
        stripeCheckoutPaymentSettings.DisplayOrder = model.DisplayOrder;

        await _settingService.SaveSetting(stripeCheckoutPaymentSettings, CurrentStoreId);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}
