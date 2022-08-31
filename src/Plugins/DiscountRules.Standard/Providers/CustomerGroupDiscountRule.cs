using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Utilities.Catalog;

namespace DiscountRules.Provider
{
    public partial class CustomerGroupDiscountRule : IDiscountRule
    {
        public CustomerGroupDiscountRule()
        {
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

            if (string.IsNullOrEmpty(request.DiscountRule.Metadata))
                return result;

            foreach (var customerGroup in request.Customer.Groups.ToList())
                if (request.DiscountRule.Metadata == customerGroup)
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
            if (!string.IsNullOrEmpty(discountRequirementId))
                result += string.Format("&discountRequirementId={0}", discountRequirementId);
            return result;
        }

        public string FriendlyName => "Must be assigned to customer group";

        public string SystemName => "DiscountRules.Standard.MustBeAssignedToCustomerGroup";
    }
}