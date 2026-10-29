using Grand.Domain.Catalog;
using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

/// <summary>
///     Represents a customer group products
/// </summary>
public class CustomerGroupProduct : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer group id
    /// </summary>
    [AuditReference(typeof(CustomerGroup))]
    public string CustomerGroupId { get; set; }

    /// <summary>
    ///     Gets or sets the product Id
    /// </summary>
    [AuditReference(typeof(Product))]
    public string ProductId { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}