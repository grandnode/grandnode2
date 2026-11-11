using Grand.Domain;
using Grand.Domain.Catalog;

namespace Grand.Business.Core.Interfaces.Catalog.Inventory;

/// <summary>
///     The filter of the inventory document list; an empty field filters nothing
/// </summary>
public sealed record InventoryDocumentSearch
{
    public InventoryDocumentType? Type { get; init; }
    public InventoryDocumentStatus? Status { get; init; }
    public string WarehouseId { get; init; }

    /// <summary>
    ///     The store of the documents' warehouses; null for every store
    /// </summary>
    public string StoreId { get; init; }

    public string Number { get; init; }

    /// <summary>
    ///     Only the return receipts of this merchandise return
    /// </summary>
    public string MerchandiseReturnId { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }
}

/// <summary>
///     Inventory documents: drafts that are edited freely, and posting, which books their stock and freezes them
/// </summary>
public interface IInventoryDocumentService
{
    Task<InventoryDocument> GetById(string id);

    Task<IPagedList<InventoryDocument>> Search(InventoryDocumentSearch search, int pageIndex = 0,
        int pageSize = int.MaxValue);

    /// <summary>
    ///     Saves a new draft with the next number of its type and year
    /// </summary>
    Task<InventoryDocument> CreateDraft(InventoryDocument document);

    /// <returns>The keys of the errors; nothing is saved when there is one</returns>
    Task<IList<string>> UpdateDraft(InventoryDocument document);

    /// <returns>The keys of the errors; nothing is deleted when there is one</returns>
    Task<IList<string>> DeleteDraft(string id);

    /// <summary>
    ///     The rules a document must meet to be posted
    /// </summary>
    /// <returns>The keys of the errors</returns>
    Task<IList<string>> Validate(InventoryDocument document);

    /// <summary>
    ///     Books the stock of every line and freezes the document. A posting that stopped part way is continued by
    ///     posting again: a line already booked is not booked twice
    /// </summary>
    /// <returns>The keys of the errors; the document stays a draft when there is one</returns>
    Task<IList<string>> Post(string id, string customerId);

    /// <summary>
    ///     A stock count draft of every bucket kept in the warehouse, with the stock stored now as expected quantity
    /// </summary>
    /// <param name="warehouseId">The warehouse counted</param>
    /// <param name="categoryIds">Only products of these categories; empty for all</param>
    /// <param name="brandIds">Only products of these brands; empty for all</param>
    /// <param name="customerId">Who creates it</param>
    /// <returns>The draft, or null and the keys of the errors</returns>
    Task<(InventoryDocument Document, IList<string> Errors)> CreateStockCount(string warehouseId,
        IList<string> categoryIds, IList<string> brandIds, string customerId);

    /// <summary>
    ///     A return receipt draft of what a merchandise return gives back and has not been received yet
    /// </summary>
    /// <returns>The draft, or null and the keys of the errors</returns>
    Task<(InventoryDocument Document, IList<string> Errors)> CreateReturnReceipt(string merchandiseReturnId,
        string customerId);

    /// <summary>
    ///     Whether goods of the merchandise return came in: a return receipt of it is posted, or a posting of one
    ///     stopped part way after booking some of its lines
    /// </summary>
    Task<bool> IsReturnReceived(string merchandiseReturnId);
}
