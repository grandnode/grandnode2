using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Infrastructure.Extensions;
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
///     notifications to subscribers. Which of them a movement triggers keeps what each source did before the journal:
///     a back-office or API edit notifies subscribers only, a shipment or an import triggers nothing.
/// </summary>
public class StockSideEffects(
    IRepository<Product> productRepository,
    IOutOfStockSubscriptionService outOfStockSubscriptionService,
    IMediator mediator,
    CatalogSettings catalogSettings)
{
    private static bool NotifiesSubscribers(InventoryMovementType movementType)
    {
        return movementType is InventoryMovementType.ManualEdit or InventoryMovementType.Api
            or InventoryMovementType.Receipt or InventoryMovementType.TransferIn
            or InventoryMovementType.ReturnReceipt or InventoryMovementType.Adjustment
            or InventoryMovementType.StockCount;
    }

    private static bool ReactsToLowStock(InventoryMovementType movementType)
    {
        return movementType is InventoryMovementType.Receipt or InventoryMovementType.TransferIn
            or InventoryMovementType.TransferOut or InventoryMovementType.ReturnReceipt
            or InventoryMovementType.Adjustment or InventoryMovementType.StockCount;
    }

    public async Task Run(Product product, IReadOnlyList<BucketChange> changes)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(changes);

        foreach (var change in changes)
        {
            if (NotifiesSubscribers(change.MovementType) && change.AvailableBefore <= 0 && change.AvailableAfter > 0)
                await NotifySubscribers(product, change);

            if (ReactsToLowStock(change.MovementType))
                await LowStock(product, change);
        }
    }

    private async Task NotifySubscribers(Product product, BucketChange change)
    {
        if (product.BackorderModeId != BackorderMode.NoBackorders || !product.AllowOutOfStockSubscriptions ||
            !product.Published)
            return;

        if (string.IsNullOrEmpty(change.CombinationId))
        {
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, change.WarehouseId);
        }
        else
        {
            await outOfStockSubscriptionService.SendNotificationsToSubscribers(product, change.Attributes,
                change.WarehouseId);
        }
    }

    private async Task LowStock(Product product, BucketChange change)
    {
        if (!string.IsNullOrEmpty(change.WarehouseId))
            return;

        if (!string.IsNullOrEmpty(change.CombinationId))
        {
            var combination = product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == change.CombinationId);
            if (combination != null && change.AvailableAfter < change.AvailableBefore &&
                change.AvailableAfter < combination.NotifyAdminForQuantityBelow)
                await QuantityBelow(product, combination);
            return;
        }

        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStock)
            return;

        if (change.AvailableAfter < change.AvailableBefore)
        {
            if (product.MinStockQuantity >= change.AvailableAfter)
                await LowStockReached(product);
            if (change.AvailableAfter < product.NotifyAdminForQuantityBelow)
                await QuantityBelow(product);
        }
        else if (catalogSettings.PublishBackProductWhenCancellingOrders &&
                 product.MinStockQuantity >= change.AvailableBefore &&
                 product.MinStockQuantity < change.AvailableAfter)
        {
            await LowStockLifted(product, change.AvailableBefore);
        }
    }

    /// <summary>
    ///     The product reached its minimum stock: its low-stock activity disables the buy button or unpublishes it
    /// </summary>
    public async Task LowStockReached(Product product)
    {
        switch (product.LowStockActivityId)
        {
            case LowStockActivity.DisableBuyButton:
                product.DisableBuyButton = true;
                product.LowStock = true;

                await productRepository.UpdateOneAsync(x => x.Id == product.Id,
                    UpdateBuilder<Product>.Create()
                        .Set(x => x.DisableBuyButton, product.DisableBuyButton)
                        .Set(x => x.LowStock, product.LowStock));

                await mediator.Publish(new ProductChangedEvent(product.Id));

                //event notification
                await mediator.EntityUpdated(product);

                break;
            case LowStockActivity.Unpublish:
                product.Published = false;
                product.LowStock = true;

                await productRepository.UpdateOneAsync(x => x.Id == product.Id,
                    UpdateBuilder<Product>.Create()
                        .Set(x => x.Published, product.Published)
                        .Set(x => x.LowStock, product.LowStock));

                await mediator.Publish(new ProductChangedEvent(product.Id, listsChanged: true));

                //event
                await mediator.Publish(new ProductUnPublishEvent(product));

                //event notification
                await mediator.EntityUpdated(product);

                break;
        }
    }

    /// <summary>
    ///     The product is above its minimum stock again: its low-stock activity is undone
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="prevStockQuantity">the stock that had reached the minimum</param>
    public async Task LowStockLifted(Product product, int prevStockQuantity)
    {
        switch (product.LowStockActivityId)
        {
            case LowStockActivity.DisableBuyButton:
                product.DisableBuyButton = false;
                product.LowStock = product.MinStockQuantity <= prevStockQuantity;

                await productRepository.UpdateOneAsync(x => x.Id == product.Id,
                    UpdateBuilder<Product>.Create()
                        .Set(x => x.DisableBuyButton, product.DisableBuyButton)
                        .Set(x => x.LowStock, product.LowStock));

                await mediator.Publish(new ProductChangedEvent(product.Id));

                //event notification
                await mediator.EntityUpdated(product);

                break;
            case LowStockActivity.Unpublish:
                product.Published = true;
                product.LowStock = product.MinStockQuantity < prevStockQuantity;

                await productRepository.UpdateOneAsync(x => x.Id == product.Id,
                    UpdateBuilder<Product>.Create()
                        .Set(x => x.Published, product.Published)
                        .Set(x => x.LowStock, product.LowStock));

                await mediator.Publish(new ProductChangedEvent(product.Id, listsChanged: true));

                //event
                await mediator.Publish(new ProductPublishEvent(product));

                //event notification
                await mediator.EntityUpdated(product);

                break;
        }
    }

    /// <summary>
    ///     Tells the store owner the stock of the product, or of its combination, fell below the quantity to notify at
    /// </summary>
    public Task QuantityBelow(Product product, ProductAttributeCombination combination = null)
    {
        return mediator.Send(new SendQuantityBelowStoreOwnerCommand {
            Product = product,
            ProductAttributeCombination = combination
        });
    }
}
