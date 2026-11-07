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

    /// <summary>
    ///     Why the change of a product is refused: switching tracking off or to the other mode, or changing the
    ///     inventory method or warehouses, while units are assigned; or tracking a product whose stock is not tracked
    /// </summary>
    /// <param name="stored">The product as stored</param>
    /// <param name="changed">The product as it is to be saved</param>
    /// <returns>The key of the error, or null</returns>
    Task<string> TrackingChangeRefused(Product stored, Product changed);
}
