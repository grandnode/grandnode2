using Grand.Domain.Audit;

namespace Grand.Domain.Catalog;

/// <summary>
///     Represents a related product
/// </summary>
public class RelatedProduct : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the second product identifier
    /// </summary>
    [AuditReference(typeof(Product))]
    [AuditName]
    public string ProductId2 { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }
}