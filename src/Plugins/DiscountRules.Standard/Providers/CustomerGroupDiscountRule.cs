using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Utilities.Catalog;

namespace DiscountRules.Standard.Providers;

public class CustomerGroupDiscountRule : IDiscountRule
{
    /// <summary>
    ///     Check discount requirement
    /// </summary>
    /// <param name="request">
    ///     Object that contains all information required to check the requirement (Current customer,
    ///     discount, etc)
    /// </param>
    /// <returns>true - requirement is met; otherwise, false</returns>
    public async Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        //invalid by default
        var result = new DiscountRuleValidationResult();

        if (request.Customer == null)
            return result;

        if (string.IsNullOrEmpty(request.DiscountRule.Metadata))
            return result;

        if (request.Customer.Groups.ToList().All(customerGroup => request.DiscountRule.Metadata != customerGroup))
            return await Task.FromResult(result);
        result.IsValid = true;
        return result;
    }

    /// <summary>
    ///     Get URL for rule configuration
    /// </summary>
    /// <param name="discountId">Discount identifier</param>
    /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
    /// <returns>URL</returns>
    public string GetConfigurationUrl(string discountId, string discountRequirementId)
    {
        //configured 
        var result = "Admin/CustomerGroups/Configure/?discountId=" + discountId;
        if (!string.IsNullOrEmpty(discountRequirementId))
            result += $"&discountRequirementId={discountRequirementId}";
        return result;
    }

    public string FriendlyName => "Must be assigned to customer group";

    public string SystemName => "DiscountRules.Standard.MustBeAssignedToCustomerGroup";
}