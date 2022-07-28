﻿using Grand.Domain.Customers;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Catalog
{
    /// <summary>
    /// Represents a discount requirement validation request
    /// </summary>
    public partial class DiscountRuleValidationRequest
    {
        /// <summary>
        /// Gets or sets the appropriate discount requirement ID (identifier)
        /// </summary>
        public string DiscountRequirementId { get; set; }

        /// <summary>
        /// Gets or sets the discount ID (identifier)
        /// </summary>
        public string DiscountId { get; set; }

        /// <summary>
        /// Gets or sets the customer
        /// </summary>
        public Customer Customer { get; set; }

        /// <summary>
        /// Gets or sets the store
        /// </summary>
        public Store Store { get; set; }
    }
}
