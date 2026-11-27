using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Domain.Stores;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Represents a discount requirement validation request
/// </summary>
public class DiscountRuleValidationRequest
{
    /// <summary>
    ///     Gets or sets the discount
    /// </summary>
    public required Discount Discount { get; set; }

    /// <summary>
    ///     Gets or sets the discount rule
    /// </summary>
    public required DiscountRule DiscountRule { get; set; }

    /// <summary>
    ///     Gets or sets the customer
    /// </summary>
    public required Customer Customer { get; set; }

    /// <summary>
    ///     Gets or sets the store
    /// </summary>
    public required Store Store { get; set; }
}