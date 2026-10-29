using Grand.Domain.Shipping;
using Grand.Domain.Stores;
using Grand.Domain.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Audit;

namespace Grand.Domain.Catalog;

/// <summary>
///     Represents a auction bid
/// </summary>
public class Bid : BaseEntity
{
    /// <summary>
    ///     Gets or sets the product identifier
    /// </summary>
    [AuditReference(typeof(Product))]
    public string ProductId { get; set; }

    /// <summary>
    ///     Gets or sets the warehouse identifier
    /// </summary>
    [AuditReference(typeof(Warehouse))]
    public string WarehouseId { get; set; }

    /// <summary>
    ///     Gets or sets the bid date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    ///     Gets or sets the customer id
    /// </summary>
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }

    /// <summary>
    ///     Gets or sets Amount
    /// </summary>
    public double Amount { get; set; }

    /// <summary>
    ///     Gets or sets Order Id
    /// </summary>
    [AuditReference(typeof(Order))]
    public string OrderId { get; set; }

    /// <summary>
    ///     /
    ///     Gets or sets Store Id
    /// </summary>
    [AuditReference(typeof(Store))]
    public string StoreId { get; set; }

    /// <summary>
    ///     Gets or sets buy it now
    /// </summary>
    public bool Bin { get; set; }

    /// <summary>
    ///     Gets or sets win bid
    /// </summary>
    public bool Win { get; set; }
}