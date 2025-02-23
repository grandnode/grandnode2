using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Controllers;

public class PaymentCashOnDeliveryController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly IContextAccessor _contextAccessor;

    public PaymentCashOnDeliveryController(
        IContextAccessor contextAccessor,
        ISettingService settingService)
    {
        _contextAccessor = contextAccessor;
        _settingService = settingService;
    }

    public async Task<IActionResult> PaymentInfo()
    {
        var cashOnDeliveryPaymentSettings = await _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(_contextAccessor.StoreContext.CurrentStore.Id);

        var model = new PaymentInfoModel {
            DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText
        };

        return View(model);
    }
}