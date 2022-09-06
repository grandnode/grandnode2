using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Controllers
{
    public class PaymentCashOnDeliveryController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;

        public PaymentCashOnDeliveryController(
            IWorkContext workContext,
            ISettingService settingService)
        {
            _workContext = workContext;
            _settingService = settingService;
        }

        public IActionResult PaymentInfo()
        {
            var cashOnDeliveryPaymentSettings = _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(_workContext.CurrentStore.Id);

            var model = new PaymentInfoModel {
                DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText
            };

            return View(model);
        }
    }
}