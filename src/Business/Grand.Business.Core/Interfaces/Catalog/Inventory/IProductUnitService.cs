using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Inventory;

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
    ///     Why the change of a product is refused: switching tracking off or to the other mode, or changing the
    ///     inventory method or warehouses, while units are assigned; or tracking a product whose stock is not tracked
    /// </summary>
    /// <param name="stored">The product as stored</param>
    /// <param name="changed">The product as it is to be saved</param>
    /// <returns>The key of the error, or null</returns>
    Task<string> TrackingChangeRefused(Product stored, Product changed);
}
