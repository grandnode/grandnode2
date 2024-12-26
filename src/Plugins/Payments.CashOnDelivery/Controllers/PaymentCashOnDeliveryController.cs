using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Controllers;

public class PaymentCashOnDeliveryController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly IWorkContextAccessor _workContextAccessor;

    public PaymentCashOnDeliveryController(
        IWorkContextAccessor workContextAccessor,
        ISettingService settingService)
    {
        _workContextAccessor = workContextAccessor;
        _settingService = settingService;
    }

    public IActionResult PaymentInfo()
    {
        var cashOnDeliveryPaymentSettings =
            _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(_workContextAccessor.WorkContext.CurrentStore.Id);

        var model = new PaymentInfoModel {
            DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText
        };

        return View(model);
    }
}