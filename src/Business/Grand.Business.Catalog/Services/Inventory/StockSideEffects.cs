using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Inventory;

/// <summary>
///     The available stock of one bucket, or of a product or combination total, before and after a stock change
/// </summary>
/// <param name="CombinationId">the combination; empty for the product</param>
/// <param name="Attributes">the combination's attributes</param>
/// <param name="WarehouseId">the warehouse row; empty for a total or a product kept in one warehouse</param>
/// <param name="AvailableBefore">stock minus reserved before the change</param>
/// <param name="AvailableAfter">stock minus reserved after the change</param>
/// <param name="MovementType">what changed it</param>
public sealed record BucketChange(
    string CombinationId,
    IList<CustomAttribute> Attributes,
    string WarehouseId,
    int AvailableBefore,
    int AvailableAfter,
    InventoryMovementType MovementType);

/// <summary>
///     What follows a stock change: the low-stock activity, the e-mail to the store owner and the back-in-stock
///     notifications to subscribers
/// </summary>
public class StockSideEffects(
    IRepository<Product> productRepository,
    IOutOfStockSubscriptionService outOfStockSubscriptionService,
    IMediator mediator,
    CatalogSettings catalogSettings)
{
    public Task Run(Product product, IReadOnlyList<BucketChange> changes)
    {
        return Task.CompletedTask;
    }
}
