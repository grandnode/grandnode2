using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.OnAccount.Models;

namespace Payments.OnAccount.Areas.Store.Controllers;

/// <summary>
///     Store-manager configuration of paying by invoice, read and written for the store the manager is bound to
/// </summary>
[Area("Store")]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentOnAccountController : BasePaymentController
{
    private readonly IContextAccessor _contextAccessor;
    private readonly ISettingService _settingService;
    private readonly IRequestTranslationService _translationService;

    public PaymentOnAccountController(
        ISettingService settingService,
        IRequestTranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _settingService = settingService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    /// <summary>
    ///     The store the current store manager is bound to; AuthorizeStore rejects a customer without one
    /// </summary>
    private string CurrentStoreId => _contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

    public async Task<IActionResult> Configure()
    {
        var settings = await _settingService.LoadSetting<OnAccountPaymentSettings>(CurrentStoreId);

        var model = new ConfigurationModel {
            BlockWhenOverdue = settings.BlockWhenOverdue,
            DisplayOrder = settings.DisplayOrder,
            PurchaseOrderNumberMode = settings.PurchaseOrderNumberMode,
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

        var settings = await _settingService.LoadSetting<OnAccountPaymentSettings>(CurrentStoreId);

        settings.BlockWhenOverdue = model.BlockWhenOverdue;
        settings.DisplayOrder = model.DisplayOrder;
        settings.PurchaseOrderNumberMode = model.PurchaseOrderNumberMode;

        await _settingService.SaveSetting(settings, CurrentStoreId);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}
