using Grand.Business.Core.Utilities.Catalog;

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
    ///     Starts the history of every stock bucket of the product's current configuration again with its stored
    ///     stock, after the inventory method or the warehouse mode changed
    /// </summary>
    Task OpenBalance(string productId, string customerId);

    /// <summary>
    ///     Works the product and combination sums out again from their buckets, after a bucket was removed
    /// </summary>
    Task RecomputeSums(string productId);
}
