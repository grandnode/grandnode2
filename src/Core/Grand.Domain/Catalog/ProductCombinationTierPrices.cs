using Grand.Domain.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Catalog;

/// <summary>
///     Manage product combination tier prices
/// </summary>
public class ProductCombinationTierPrices : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the store identifier
    /// </summary>
    [AuditReference(typeof(Store))]
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the customer group identifier
    /// </summary>
    [AuditReference(typeof(CustomerGroup))]
    public string CustomerGroupId { get; set; }

    /// <summary>
    ///     Gets or sets the quantity
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    ///     Gets or sets the price
    /// </summary>
    public double Price { get; set; }
}