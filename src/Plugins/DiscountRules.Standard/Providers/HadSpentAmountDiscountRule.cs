using DiscountRules.Standard.Models;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscountRules.Provider
{
    public partial class HadSpentAmountDiscountRule : IDiscountRule
    {
        private readonly ISettingService _settingService;
        private readonly IOrderService _orderService;
        private readonly ITranslationService _translationService;

        public HadSpentAmountDiscountRule(ISettingService settingService, IOrderService orderService, ITranslationService translationService)
        {
            _settingService = settingService;
            _orderService = orderService;
            _translationService = translationService;
        }

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>true - requirement is met; otherwise, false</returns>
        public async Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //invalid by default
            var result = new DiscountRuleValidationResult();

            var spentAmountRequirement = _settingService.GetSettingByKey<RequirementHadSpentAmount>(string.Format("DiscountRules.Standard.HadSpentAmount-{0}-{1}", request.DiscountId, request.DiscountRequirementId));

            if (spentAmountRequirement == null || spentAmountRequirement.SpentAmount == 0)
            {
                //valid
                result.IsValid = true;
                return result;
            }

            if (request.Customer == null)
                return result;

            var orders = await _orderService.SearchOrders(storeId: request.Store.Id,
                customerId: request.Customer.Id,
                os: (int)OrderStatusSystem.Complete);
            double spentAmount = orders.Sum(o => o.OrderTotal);
            if (spentAmount > spentAmountRequirement.SpentAmount)
            {
                result.IsValid = true;
            }
            else
            {
                result.UserError = _translationService.GetResource("Plugins.DiscountRules.Standard.HadSpentAmount.NotEnough");
            }
            return result;
        }

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(string discountId, string discountRequirementId)
        {
            //configured 
            string result = "Admin/HadSpentAmount/Configure/?discountId=" + discountId;
            if (!String.IsNullOrEmpty(discountRequirementId))
                result += string.Format("&discountRequirementId={0}", discountRequirementId);
            return result;
        }

        public string FriendlyName => "Customer had spent x.xx amount";

        public string SystemName => "DiscountRules.Standard.HadSpentAmount";
    }
}