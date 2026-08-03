namespace Grand.Domain.Tax;

/// <summary>
///     Represents a tax category
/// </summary>
public class TaxCategory : BaseEntity
{
    /// <summary>
    ///     Gets or sets the name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the display order
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     Gets or sets the store identifier (empty = available to all stores)
    /// </summary>
    public string StoreId { get; set; }
}