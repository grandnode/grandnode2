using Grand.Domain.Catalog;
using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

/// <summary>
///     Represents a customer tag product
/// </summary>
public class CustomerTagProduct : BaseEntity
{
    /// <summary>
    ///     Gets or sets the customer tag id
    /// </summary>
    [AuditReference(typeof(CustomerTag))]
    public string CustomerTagId { get; set; }

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