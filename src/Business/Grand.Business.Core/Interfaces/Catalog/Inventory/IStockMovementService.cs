using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Inventory;

/// <summary>
///     The one path for changing physical stock: each change is applied to the stored product under its lock and
///     written to the inventory journal
/// </summary>
public interface IStockMovementService
{
    /// <summary>
    ///     Applies the changes of one product under its lock
    /// </summary>
    /// <returns>The keys of the errors; nothing is written when there is one</returns>
    Task<IList<string>> Apply(string productId, IReadOnlyList<StockChange> changes);

    /// <summary>
    ///     Works the product and combination sums out again from their buckets, after a bucket was removed
    /// </summary>
    Task RecomputeSums(string productId);

    /// <summary>
    ///     Journals the stock a new product starts with; called before the product is inserted
    /// </summary>
    /// <param name="product">A product not inserted yet; it gets the revision of its last entry</param>
    /// <param name="movementType">What created the product</param>
    /// <param name="source">The source the entries name</param>
    Task JournalNewProduct(Product product, InventoryMovementType movementType, StockSource source);
}
