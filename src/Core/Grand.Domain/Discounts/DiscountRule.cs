namespace Grand.Domain.Discounts;

/// <summary>
///     Represents a discount requirement
/// </summary>
public class DiscountRule : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the discount requirement rule system name
    /// </summary>
    public string DiscountRequirementRuleSystemName { get; set; }

    /// <summary>
    ///     Gets or sets the metadata
    /// </summary>
    public string Metadata { get; set; }
}