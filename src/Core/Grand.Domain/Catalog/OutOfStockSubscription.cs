using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Customers;
using Grand.Domain.Audit;
using Grand.Domain.Common;

namespace Grand.Domain.Catalog;

/// <summary>
///     Represents a out of stock subscription
/// </summary>
public class OutOfStockSubscription : BaseEntity
{
    /// <summary>
    ///     Gets or sets the store identifier
    /// </summary>
    [AuditReference(typeof(Store))]
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets the product identifier
    /// </summary>
    [AuditReference(typeof(Product))]
    public string ProductId { get; set; }

    /// <summary>
    ///     Gets or sets the attribute
    /// </summary>
    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     Gets or sets the attribute info (format)
    /// </summary>
    public string AttributeInfo { get; set; }

    /// <summary>
    ///     Gets or sets the warehouse identifier
    /// </summary>
    [AuditReference(typeof(Warehouse))]
    public string WarehouseId { get; set; }

    /// <summary>
    ///     Gets or sets the customer identifier
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }
}