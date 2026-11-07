using Grand.Domain.Catalog;

namespace Grand.Data.Inventory;

/// <summary>
///     The indexes of the inventory journal and the inventory documents, shared by the installer and the 2.5 migration
/// </summary>
public static class InventoryIndexes
{
    public static async Task Create(IDatabaseContext databaseContext, IRepository<InventoryJournal> journal,
        IRepository<InventoryDocument> documents)
    {
        await databaseContext.CreateIndex(journal,
            OrderBuilder<InventoryJournal>.Create().Ascending(x => x.ProductId).Descending(x => x.CreateDateUtc),
            "ProductId_CreateDateUtc");
        await databaseContext.CreateIndex(journal,
            OrderBuilder<InventoryJournal>.Create().Ascending(x => x.ProductId).Ascending(x => x.StockRevision),
            "ProductId_StockRevision");
        await databaseContext.CreateIndex(journal,
            OrderBuilder<InventoryJournal>.Create().Ascending(x => x.WarehouseId).Descending(x => x.CreateDateUtc),
            "WarehouseId_CreateDateUtc");
        await databaseContext.CreateIndex(journal,
            OrderBuilder<InventoryJournal>.Create().Ascending(x => x.ObjectType).Ascending(x => x.ObjectId)
                .Ascending(x => x.ProductId), "ObjectType_ObjectId_ProductId");
        await databaseContext.CreateIndex(journal,
            OrderBuilder<InventoryJournal>.Create().Ascending(x => x.PositionId), "PositionId");

        //the backstop of the numbering: one number per type, year and sequence
        await databaseContext.CreateIndex(documents,
            OrderBuilder<InventoryDocument>.Create().Ascending(x => x.DocumentTypeId).Ascending(x => x.Year)
                .Ascending(x => x.Sequence), "DocumentTypeId_Year_Sequence", true);
        await databaseContext.CreateIndex(documents,
            OrderBuilder<InventoryDocument>.Create().Ascending(x => x.StoreId).Descending(x => x.CreatedOnUtc),
            "StoreId_CreatedOnUtc");
        await databaseContext.CreateIndex(documents,
            OrderBuilder<InventoryDocument>.Create().Ascending(x => x.WarehouseId).Descending(x => x.CreatedOnUtc),
            "WarehouseId_CreatedOnUtc");
    }

    /// <summary>
    ///     The indexes of batches and serial numbers. The unique ones are the backstop of the numbering: the stock
    ///     ledger checks it under the product's lock before every write
    /// </summary>
    public static async Task CreateUnitIndexes(IDatabaseContext databaseContext, IRepository<ProductBatch> batches,
        IRepository<ProductSerialNumber> serialNumbers, IRepository<InventoryJournal> journal)
    {
        await databaseContext.CreateIndex(batches,
            OrderBuilder<ProductBatch>.Create().Ascending(x => x.ProductId).Ascending(x => x.CombinationId)
                .Ascending(x => x.WarehouseId).Ascending(x => x.NumberKey),
            "ProductId_CombinationId_WarehouseId_NumberKey", true);
        await databaseContext.CreateIndex(batches,
            OrderBuilder<ProductBatch>.Create().Ascending(x => x.ExpiryDateUtc), "ExpiryDateUtc");
        await databaseContext.CreateIndex(batches,
            OrderBuilder<ProductBatch>.Create().Ascending(x => x.NumberKey), "NumberKey");
        //the store manager's list and dashboard
        await databaseContext.CreateIndex(batches,
            OrderBuilder<ProductBatch>.Create().Ascending(x => x.StoreId).Ascending(x => x.ExpiryDateUtc),
            "StoreId_ExpiryDateUtc");

        await databaseContext.CreateIndex(serialNumbers,
            OrderBuilder<ProductSerialNumber>.Create().Ascending(x => x.ProductId).Ascending(x => x.NumberKey),
            "ProductId_NumberKey", true);
        await databaseContext.CreateIndex(serialNumbers,
            OrderBuilder<ProductSerialNumber>.Create().Ascending(x => x.NumberKey), "NumberKey");
        await databaseContext.CreateIndex(serialNumbers,
            OrderBuilder<ProductSerialNumber>.Create().Ascending(x => x.OrderId), "OrderId");
        //the store manager's list
        await databaseContext.CreateIndex(serialNumbers,
            OrderBuilder<ProductSerialNumber>.Create().Ascending(x => x.StoreId).Ascending(x => x.NumberKey),
            "StoreId_NumberKey");

        //the history and the recipients of a batch, a page at a time
        await databaseContext.CreateIndex(journal,
            OrderBuilder<InventoryJournal>.Create().Ascending(x => x.ProductId).Ascending(x => x.BatchNumber)
                .Ascending(x => x.StockRevision), "ProductId_BatchNumber_StockRevision");
    }
}
