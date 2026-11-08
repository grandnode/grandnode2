using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Catalog.Inventory;

/// <summary>
///     What a shipment takes from one batch; an empty number is the stock without a batch
/// </summary>
/// <param name="Number">The batch number; empty for the stock without a batch</param>
/// <param name="ExpiryDateUtc">The batch's expiry date</param>
/// <param name="Status">Its expiry status today; null without an expiry date</param>
/// <param name="Available">The quantity the batch holds in the warehouse</param>
/// <param name="Quantity">The quantity proposed from it</param>
public sealed record BatchAllocation(
    string Number,
    DateTime? ExpiryDateUtc,
    BatchExpiryStatus? Status,
    int Available,
    int Quantity);

/// <summary>
///     Why the units of a shipment item cannot be shipped
/// </summary>
public sealed record ShipmentUnitError(string ShipmentItemId, string ProductName, string ErrorKey);

/// <summary>
///     The filter of the batch and serial number screens; an empty field filters nothing
/// </summary>
public sealed record UnitSearch
{
    /// <summary>
    ///     The number, compared without case
    /// </summary>
    public string Number { get; init; }

    /// <summary>
    ///     The number is the start of the numbers found
    /// </summary>
    public bool Prefix { get; init; }

    public string ProductId { get; init; }
    public SerialNumberStatus? Status { get; init; }
    public string WarehouseId { get; init; }

    /// <summary>
    ///     The store of the units' warehouses; null for every store
    /// </summary>
    public string StoreId { get; init; }

    public BatchExpiryStatus? ExpiryStatus { get; init; }

    /// <summary>
    ///     Only batches holding stock
    /// </summary>
    public bool WithStockOnly { get; init; }

    /// <summary>
    ///     The date the expiry status is worked out at, in the store's time zone
    /// </summary>
    public DateTime Today { get; init; }
}

/// <summary>
///     A batch holding stock that became expiring or expired since the store owner was last told
/// </summary>
public sealed record ExpiringBatch(
    ProductBatch Batch,
    string ProductName,
    string Sku,
    string WarehouseName,
    BatchExpiryStatus Status);

/// <summary>
///     A shipment that shipped units of a batch: who received them
/// </summary>
public sealed record BatchRecipient(
    string ShipmentId,
    int ShipmentNumber,
    string OrderId,
    int OrderNumber,
    string CustomerId,
    string CustomerEmail,
    int Quantity,
    DateTime ShippedOnUtc);

/// <summary>
///     One stock bucket of a product tracking units: its stock and the part of it batches or serial numbers hold
/// </summary>
public sealed record UnitBucketView(
    string CombinationId,
    IList<CustomAttribute> Attributes,
    string WarehouseId,
    int Stock,
    int Assigned)
{
    /// <summary>
    ///     The stock without units
    /// </summary>
    public int Pool => Stock - Assigned;
}

/// <summary>
///     The batches and serial numbers products hold: what is assigned, and which changes of a product they block
/// </summary>
public interface IProductUnitService
{
    /// <summary>
    ///     The units assigned to the product's stock: the quantity of its batches and its serial numbers in stock
    /// </summary>
    /// <param name="productId">The product</param>
    /// <param name="combinationId">Only this combination; null for the whole product</param>
    Task<int> AssignedQuantity(string productId, string combinationId = null);

    /// <summary>
    ///     The units assigned to stock kept in the warehouse, of every product
    /// </summary>
    Task<int> AssignedInWarehouse(string warehouseId);

    /// <summary>
    ///     The batches holding stock in one bucket, earliest expiry first, batches without one last
    /// </summary>
    /// <param name="productId">The product</param>
    /// <param name="combinationId">The combination; empty for a product without one</param>
    /// <param name="warehouseId">The warehouse the journal names for the bucket</param>
    Task<IList<ProductBatch>> Batches(string productId, string combinationId, string warehouseId);

    /// <summary>
    ///     How a shipment of the quantity takes from the warehouse's batches: valid batches first, the one expiring
    ///     first before the others and those without an expiry date last, expired batches only when the others do
    ///     not cover the quantity, then the stock without a batch, beyond it only for a product allowing backorders
    /// </summary>
    /// <param name="product">A batch-tracked product</param>
    /// <param name="attributes">The combination shipped</param>
    /// <param name="warehouseId">The warehouse shipped from</param>
    /// <param name="quantity">The quantity shipped</param>
    /// <param name="today">The date in the store's time zone</param>
    /// <returns>Every batch holding stock and the stock without a batch, in that order, with what is taken</returns>
    Task<IList<BatchAllocation>> ProposeBatches(Product product, IList<CustomAttribute> attributes,
        string warehouseId, int quantity, DateTime today);

    /// <summary>
    ///     Checks the units every item of a shipment not stored yet names, against the units stored now, and fixes the
    ///     warranty of the units shipped. An expired batch and stock without a batch are allowed: they are warnings
    /// </summary>
    /// <returns>The errors, one per item at most; the shipment is not to be stored when there is one</returns>
    Task<IList<ShipmentUnitError>> PrepareShipmentUnits(Shipment shipment);

    Task<IPagedList<ProductSerialNumber>> SearchSerialNumbers(UnitSearch search, int pageIndex = 0,
        int pageSize = int.MaxValue);

    Task<IPagedList<ProductBatch>> SearchBatches(UnitSearch search, int pageIndex = 0, int pageSize = int.MaxValue);

    Task<ProductSerialNumber> GetSerialNumber(string id);

    Task<ProductBatch> GetBatch(string id);

    /// <summary>
    ///     A page of the journal entries of the batch, in its warehouse, newest first
    /// </summary>
    Task<IPagedList<InventoryJournal>> History(ProductBatch batch, int pageIndex, int pageSize);

    /// <summary>
    ///     A page of the journal entries naming the serial number, newest first
    /// </summary>
    /// <param name="serialNumber">The serial number</param>
    /// <param name="warehouseIds">The warehouses whose entries are read; null for every warehouse it passed through</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    Task<IPagedList<InventoryJournal>> History(ProductSerialNumber serialNumber, IList<string> warehouseIds,
        int pageIndex, int pageSize);

    /// <summary>
    ///     A page of the shipments that shipped units of the batch from its warehouse and kept them, with their order
    ///     and customer, in shipping order
    /// </summary>
    Task<IPagedList<BatchRecipient>> BatchRecipients(ProductBatch batch, int pageIndex, int pageSize);

    /// <summary>
    ///     The stock buckets of a product tracking units, with the stock batches or serial numbers hold
    /// </summary>
    Task<IList<UnitBucketView>> Buckets(Product product);

    /// <summary>
    ///     The batches holding stock whose expiry status today is expiring or expired and was not e-mailed yet,
    ///     earliest expiry first
    /// </summary>
    /// <param name="today">The date in the store's time zone</param>
    Task<IList<ExpiringBatch>> BatchesToNotify(DateTime today);

    /// <summary>
    ///     Records the status each batch was e-mailed with
    /// </summary>
    Task MarkNotified(IEnumerable<ExpiringBatch> batches);

    /// <summary>
    ///     How many batches holding stock are expiring and expired, by the inventory setting's warning days
    /// </summary>
    /// <param name="storeId">The store of their warehouses; null for every store</param>
    /// <param name="today">The date in the store's time zone</param>
    Task<(int Expiring, int Expired)> ExpiryCounts(string storeId, DateTime today);

    /// <summary>
    ///     Why the change of a product is refused: switching tracking off or to the other mode, or changing the
    ///     inventory method or warehouses, while units are assigned; or tracking a product whose stock is not tracked
    /// </summary>
    /// <param name="stored">The product as stored</param>
    /// <param name="changed">The product as it is to be saved</param>
    /// <returns>The key of the error, or null</returns>
    Task<string> TrackingChangeRefused(Product stored, Product changed);
}
