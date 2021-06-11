using Grand.Business.Common.Interfaces.Configuration;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;

namespace Payments.CashOnDelivery.Components
{
    public class PaymentCashOnDeliveryViewComponent : BaseViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;

        public PaymentCashOnDeliveryViewComponent(
            IWorkContext workContext,
            ISettingService settingService)
        {
            _workContext = workContext;
            _settingService = settingService;
        }

        public IViewComponentResult Invoke()
        {
            var cashOnDeliveryPaymentSettings = _settingService.LoadSetting<CashOnDeliveryPaymentSettings>(_workContext.CurrentStore.Id);

            var model = new PaymentInfoModel {
                DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText
            };
            return View(this.GetViewPath(), model);
        }
    }
}