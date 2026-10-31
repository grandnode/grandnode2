using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.OnAccount.Models;

namespace Payments.OnAccount.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentOnAccountController : BasePaymentController
{
    private readonly IAdminStoreService _adminStoreService;
    private readonly ISettingService _settingService;
    private readonly IRequestTranslationService _translationService;

    public PaymentOnAccountController(
        ISettingService settingService,
        IRequestTranslationService translationService,
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
        var settings = await _settingService.LoadSetting<OnAccountPaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            BlockWhenOverdue = settings.BlockWhenOverdue,
            DisplayOrder = settings.DisplayOrder,
            PurchaseOrderNumberMode = settings.PurchaseOrderNumberMode,
            ActiveStore = storeScope
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
        var settings = await _settingService.LoadSetting<OnAccountPaymentSettings>(storeScope);

        settings.BlockWhenOverdue = model.BlockWhenOverdue;
        settings.DisplayOrder = model.DisplayOrder;
        settings.PurchaseOrderNumberMode = model.PurchaseOrderNumberMode;

        await _settingService.SaveSetting(settings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}
