using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Services.Inventory;

/// <summary>
///     Takes the product's lock, reads the stored product and hands it to the <see cref="StockLedger" />
/// </summary>
public class StockMovementService(
    IResourceLock resourceLock,
    IRepository<Product> productRepository,
    StockLedger stockLedger) : IStockMovementService
{
    public async Task<IList<string>> Apply(string productId, IReadOnlyList<StockChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);

        await using var _ = await resourceLock.Acquire([ProductStockLock.Key(productId)]);

        var product = await productRepository.GetByIdAsync(productId);
        return product == null ? [StockErrors.ProductNotFound] : await stockLedger.Apply(product, changes);
    }

    public async Task OpenBalance(string productId, string customerId)
    {
        await using var _ = await resourceLock.Acquire([ProductStockLock.Key(productId)]);

        var product = await productRepository.GetByIdAsync(productId);
        if (product != null)
            await stockLedger.OpenBalance(product, customerId);
    }

    public async Task RecomputeSums(string productId)
    {
        await using var _ = await resourceLock.Acquire([ProductStockLock.Key(productId)]);

        var product = await productRepository.GetByIdAsync(productId);
        if (product != null)
            await stockLedger.RecomputeSums(product);
    }
}
