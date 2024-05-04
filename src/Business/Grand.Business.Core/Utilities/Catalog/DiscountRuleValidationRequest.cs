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
    public Discount Discount { get; set; }

    /// <summary>
    ///     Gets or sets the discount rule
    /// </summary>
    public DiscountRule DiscountRule { get; set; }

    /// <summary>
    ///     Gets or sets the customer
    /// </summary>
    public Customer Customer { get; set; }

    /// <summary>
    ///     Gets or sets the store
    /// </summary>
    public Store Store { get; set; }
}