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
}
