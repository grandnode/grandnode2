using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;

namespace Grand.Business.Core.Interfaces.Catalog.Inventory;

/// <summary>
///     The stock one bucket of a warehouse held at a date
/// </summary>
/// <param name="ProductId">the product</param>
/// <param name="ProductName">its name</param>
/// <param name="Sku">its SKU</param>
/// <param name="CombinationId">the combination; empty for the product</param>
/// <param name="Attributes">the combination's attributes</param>
/// <param name="Stock">the stock at the date</param>
/// <param name="Available">false when the date is before the bucket's history starts</param>
public sealed record StockAtDateRow(
    string ProductId,
    string ProductName,
    string Sku,
    string CombinationId,
    IList<CustomAttribute> Attributes,
    int Stock,
    bool Available);

/// <summary>
///     Reads of the inventory journal
/// </summary>
public interface IInventoryJournalService
{
    /// <summary>
    ///     The journal entries of a product, newest first
    /// </summary>
    /// <param name="productId">The product</param>
    /// <param name="warehouseId">Only this warehouse; empty for all</param>
    /// <param name="fromUtc">Not before</param>
    /// <param name="toUtc">Not after</param>
    /// <param name="storeIds">Only entries of these stores (empty for a shared warehouse); null for all</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    Task<IPagedList<InventoryJournal>> GetProductHistory(string productId, string warehouseId, DateTime? fromUtc,
        DateTime? toUtc, IList<string> storeIds, int pageIndex, int pageSize);

    /// <summary>
    ///     The stock of every bucket kept in the warehouse at a date: the stored stock less what changed since
    /// </summary>
    /// <param name="warehouseId">The warehouse</param>
    /// <param name="atUtc">The date</param>
    /// <param name="storeId">Only products a store sees: those not limited to stores and its own; null for all</param>
    Task<IList<StockAtDateRow>> GetStockAtDate(string warehouseId, DateTime atUtc, string storeId);
}
