using DiscountRules.Standard.Models;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Common.Interfaces.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscountRules.Provider
{
    public partial class CustomerGroupDiscountRule : IDiscountRule
    {
        private readonly ISettingService _settingService;

        public CustomerGroupDiscountRule(ISettingService settingService)
        {
            _settingService = settingService;
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

            if (request.Customer == null)
                return result;

            var restrictedToCustomerGroupId = _settingService.GetSettingByKey<RequirementCustomerGroup>(string.Format("DiscountRules.Standard.MustBeAssignedToCustomerGroup-{0}-{1}", request.DiscountId, request.DiscountRequirementId));

            if (restrictedToCustomerGroupId == null || String.IsNullOrEmpty(restrictedToCustomerGroupId?.CustomerGroupId))
                return result;

            foreach (var customerGroup in request.Customer.Groups.ToList())
                if (restrictedToCustomerGroupId.CustomerGroupId == customerGroup)
                {
                    //valid
                    result.IsValid = true;
                    return result;
                }

            return await Task.FromResult(result);
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
            string result = "Admin/CustomerGroups/Configure/?discountId=" + discountId;
            if (!String.IsNullOrEmpty(discountRequirementId))
                result += string.Format("&discountRequirementId={0}", discountRequirementId);
            return result;
        }

        public string FriendlyName => "Must be assigned to customer group";

        public string SystemName => "DiscountRules.Standard.MustBeAssignedToCustomerGroup";
    }
}