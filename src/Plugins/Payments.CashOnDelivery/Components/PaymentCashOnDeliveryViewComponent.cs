using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Web.Common.Components;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Payments.CashOnDelivery.Models;
using System.Threading.Tasks;

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

            var model = new PaymentInfoModel
            {
                DescriptionText = cashOnDeliveryPaymentSettings.DescriptionText
            };
            return View("~/Plugins/Payments.CashOnDelivery/Views/PaymentCashOnDelivery/PaymentInfo.cshtml", model);
        }
    }
}