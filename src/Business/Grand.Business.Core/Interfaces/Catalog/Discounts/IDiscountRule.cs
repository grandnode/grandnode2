﻿using Grand.Business.Core.Utilities.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Discounts
{
    public interface IDiscountRule
    {
        /// <summary>
        /// Check discount requirements
        /// </summary>
        /// <param name="request">All information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>Result</returns>
        Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request);

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount id</param>
        /// <param name="discountRequirementId">Discount requirement id</param>
        /// <returns>URL</returns>
        string GetConfigurationUrl(string discountId, string discountRequirementId);

        /// <summary>
        /// Gets a system name
        /// </summary>
        /// <returns>SystemName</returns>
        string SystemName { get; }

        /// <summary>
        /// Gets a friendly name
        /// </summary>
        /// <returns>FriendlyName</returns>
        string FriendlyName { get; }
    }
}
