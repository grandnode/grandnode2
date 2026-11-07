using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    /// <summary>
    ///     Every stock change is journaled from the start, the stock of the sample products included
    /// </summary>
    protected virtual async Task JournalSampleStock()
    {
        var warehouseStores = _warehouseRepository.Table.ToList().ToDictionary(x => x.Id, x => x.StoreId ?? "");
        var products = _productRepository.Table.Where(x =>
            x.ManageInventoryMethodId == ManageInventoryMethod.ManageStock ||
            x.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes).ToList();
        foreach (var product in products)
        {
            var entries = StockBuckets.InitialEntries(product, InventoryMovementType.ManualEdit,
                new StockSource(nameof(Product), product.Id, Reference: product.Sku ?? "", Comment: "Sample data"),
                warehouseStores);
            if (entries.Count == 0)
                continue;

            foreach (var entry in entries)
                await _inventoryJournalRepository.InsertAsync(entry);
            await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                UpdateBuilder<Product>.Create().Set(x => x.StockRevision, product.StockRevision));
        }
    }
}
