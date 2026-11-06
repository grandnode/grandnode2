using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Products;

public class InventoryManageService : IInventoryManageService
{
    #region Ctor

    public InventoryManageService(
        IRepository<Product> productRepository,
        IRepository<InventoryJournal> inventoryJournalRepository,
        IStockQuantityService stockQuantityService,
        IMediator mediator,
        CatalogSettings catalogSettings,
        IResourceLock resourceLock)
    {
        _productRepository = productRepository;
        _inventoryJournalRepository = inventoryJournalRepository;
        _stockQuantityService = stockQuantityService;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
        _resourceLock = resourceLock;
    }

    #endregion

    #region Fields

    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<InventoryJournal> _inventoryJournalRepository;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly IMediator _mediator;
    private readonly CatalogSettings _catalogSettings;
    private readonly IResourceLock _resourceLock;

    #endregion

    #region Utilities methods

    private async Task ManageStockInventory(Product product, ShipmentItem shipmentItem)
    {
        if (product.UseMultipleWarehouses)
        {
            var pwi = product.ProductWarehouseInventory.FirstOrDefault(pi =>
                pi.WarehouseId == shipmentItem.WarehouseId);
            if (pwi == null)
                return;

            pwi.ReservedQuantity -= shipmentItem.Quantity;
            pwi.StockQuantity -= shipmentItem.Quantity;
            if (pwi.ReservedQuantity < 0)
                pwi.ReservedQuantity = 0;

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductWarehouseInventory, z => z.Id == pwi.Id, pwi);
            product.StockQuantity = product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
            product.ReservedQuantity = product.ProductWarehouseInventory.Sum(x => x.ReservedQuantity);
            await UpdateStockProduct(product);
        }
        else
        {
            product.ReservedQuantity -= shipmentItem.Quantity;
            product.StockQuantity -= shipmentItem.Quantity;
            await UpdateStockProduct(product);
        }
    }

    private async Task ManageStockByAttributesInventory(Product product, ShipmentItem shipmentItem)
    {
        var combination = product.FindProductAttributeCombination(shipmentItem.Attributes);
        if (combination == null)
            return;

        if (!product.UseMultipleWarehouses)
        {
            combination.ReservedQuantity -= shipmentItem.Quantity;
            combination.StockQuantity -= shipmentItem.Quantity;
            if (combination.ReservedQuantity < 0)
                combination.ReservedQuantity = 0;

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
        }
        else
        {
            var pwi = combination.WarehouseInventory.FirstOrDefault(pi => pi.WarehouseId == shipmentItem.WarehouseId);
            if (pwi == null)
                return;

            pwi.ReservedQuantity -= shipmentItem.Quantity;
            pwi.StockQuantity -= shipmentItem.Quantity;
            if (pwi.ReservedQuantity < 0)
                pwi.ReservedQuantity = 0;

            combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
            combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
        }

        product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
        product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
        await UpdateStockProduct(product);
    }

    private async Task ManageStockByBundleProductsInventory(Product product, Shipment shipment,
        ShipmentItem shipmentItem)
    {
        foreach (var item in product.BundleProducts)
        {
            var p1 = await _productRepository.GetByIdAsync(item.ProductId);
            if (p1 == null || p1.Id == product.Id ||
                p1.ManageInventoryMethodId == ManageInventoryMethod.DontManageStock) continue;
            var shipmentItem1 = new ShipmentItem {
                Id = shipmentItem.Id,
                Attributes = shipmentItem.Attributes,
                OrderItemId = shipmentItem.OrderItemId,
                ProductId = shipmentItem.ProductId,
                Quantity = shipmentItem.Quantity * item.Quantity,
                WarehouseId = shipmentItem.WarehouseId
            };
            if (!await CheckExistsInventoryJournal(p1, shipmentItem1))
                await BookReservedInventoryCore(p1, shipment, shipmentItem1);
        }
    }

    private async Task ManageAttributesInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
    {
        var attributeValues = product.ParseProductAttributeValues(shipmentItem.Attributes);
        foreach (var attributeValue in attributeValues)
        {
            if (attributeValue.AttributeValueTypeId != AttributeValueType.AssociatedToProduct) continue;
            //associated product
            var associatedProduct = await _productRepository.GetByIdAsync(attributeValue.AssociatedProductId);
            if (associatedProduct == null
                || associatedProduct.Id == product.Id
                || associatedProduct.ManageInventoryMethodId == ManageInventoryMethod.DontManageStock) continue;
            if (await CheckExistsInventoryJournal(associatedProduct, shipmentItem)) continue;
            var item = new ShipmentItem {
                Id = shipmentItem.Id,
                Attributes = shipmentItem.Attributes,
                OrderItemId = shipmentItem.OrderItemId,
                ProductId = shipmentItem.ProductId,
                Quantity = shipmentItem.Quantity * attributeValue.Quantity,
                WarehouseId = shipmentItem.WarehouseId
            };
            await BookReservedInventoryCore(associatedProduct, shipment, item);
        }
    }

    private async Task<bool> CheckExistsInventoryJournal(Product product, ShipmentItem shipmentItem)
    {
        var query = from j in _inventoryJournalRepository.Table
            where j.ProductId == product.Id && j.PositionId == shipmentItem.Id
            select j.Id;

        return await _inventoryJournalRepository.AnyAsync(query);
    }

    private async Task ReverseBookedInventory(Product product, InventoryJournal inventoryJournal)
    {
        switch (product.ManageInventoryMethodId)
        {
            //standard manage stock
            case ManageInventoryMethod.ManageStock when product.UseMultipleWarehouses:
            {
                var pwi = product.ProductWarehouseInventory.FirstOrDefault(x =>
                    x.WarehouseId == inventoryJournal.WarehouseId);
                if (pwi == null)
                    return;

                pwi.StockQuantity += inventoryJournal.OutQty;
                pwi.ReservedQuantity += inventoryJournal.OutQty;
                if (pwi.ReservedQuantity < 0)
                    pwi.ReservedQuantity = 0;

                await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductWarehouseInventory, z => z.Id == pwi.Id, pwi);
                break;
            }
            case ManageInventoryMethod.ManageStock:
                product.StockQuantity += inventoryJournal.OutQty;
                product.ReservedQuantity += inventoryJournal.OutQty;
                await UpdateStockProduct(product);
                break;
            //manage stock by attributes
            case ManageInventoryMethod.ManageStockByAttributes:
            {
                var combination = product.FindProductAttributeCombination(inventoryJournal.Attributes);
                if (combination == null)
                    return;

                if (!product.UseMultipleWarehouses)
                {
                    combination.StockQuantity += inventoryJournal.OutQty;
                    combination.ReservedQuantity += inventoryJournal.OutQty;
                    if (combination.ReservedQuantity < 0)
                        combination.ReservedQuantity = 0;

                    product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
                    product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

                    await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
                    await UpdateStockProduct(product);
                }
                else
                {
                    var pwi = combination.WarehouseInventory.FirstOrDefault(x =>
                        x.WarehouseId == inventoryJournal.WarehouseId);
                    if (pwi == null)
                        return;

                    pwi.StockQuantity += inventoryJournal.OutQty;
                    pwi.ReservedQuantity += inventoryJournal.OutQty;

                    if (pwi.ReservedQuantity < 0)
                        pwi.ReservedQuantity = 0;

                    combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
                    combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);
                    product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
                    product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

                    await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
                    await UpdateStockProduct(product);
                }

                break;
            }
        }

        await _inventoryJournalRepository.DeleteAsync(inventoryJournal);
    }

    private async Task InsertInventoryJournal(Product product, Shipment shipment, ShipmentItem shipmentItem)
    {
        var ij = new InventoryJournal {
            CreateDateUtc = DateTime.UtcNow,
            ObjectType = typeof(Shipment).Name,
            ObjectId = shipment.Id,
            PositionId = shipmentItem.Id,
            Attributes = shipmentItem.Attributes,
            ProductId = product.Id,
            WarehouseId = shipmentItem.WarehouseId,
            Reference = shipment.ShipmentNumber.ToString(),
            Comments = $"Shipment - {shipment.TrackingNumber}",
            OutQty = shipmentItem.Quantity
        };
        await _inventoryJournalRepository.InsertAsync(ij);
    }

    /// <summary>
    ///     The keys of every product a stock write for this product touches: its bundle products and the products
    ///     associated with the chosen attribute values
    /// </summary>
    private static IEnumerable<string> ProductKeys(Product product, IList<CustomAttribute> attributes)
    {
        yield return LockKey(product.Id);

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            foreach (var item in product.BundleProducts)
                yield return LockKey(item.ProductId);

        foreach (var attributeValue in product.ParseProductAttributeValues(attributes))
            if (attributeValue.AttributeValueTypeId == AttributeValueType.AssociatedToProduct)
                yield return LockKey(attributeValue.AssociatedProductId);
    }

    private static string LockKey(string productId)
    {
        return $"{nameof(Product)}:{productId}";
    }

    /// <summary>
    ///     The warehouse to count the stock of: a product kept in one warehouse has all its stock there, also when the
    ///     order item names the store's default warehouse
    /// </summary>
    private static string StockWarehouse(Product product, string warehouseId)
    {
        return product.UseMultipleWarehouses ? warehouseId : "";
    }

    /// <summary>
    ///     Whether the stored stock covers the quantity, by the rule the shopping cart applies
    /// </summary>
    private async Task<bool> IsAvailable(Product product, int quantity, IList<CustomAttribute> attributes,
        string warehouseId)
    {
        switch (product.ManageInventoryMethodId)
        {
            case ManageInventoryMethod.ManageStock:
                return product.BackorderModeId != BackorderMode.NoBackorders ||
                       _stockQuantityService.GetTotalStockQuantity(product,
                           warehouseId: StockWarehouse(product, warehouseId)) >= quantity;
            case ManageInventoryMethod.ManageStockByAttributes:
            {
                var combination = product.FindProductAttributeCombination(attributes);
                return combination != null && (combination.AllowOutOfStockOrders ||
                                               _stockQuantityService.GetTotalStockQuantityForCombination(product,
                                                   combination,
                                                   warehouseId: StockWarehouse(product, warehouseId)) >= quantity);
            }
            case ManageInventoryMethod.ManageStockByBundleProducts:
                foreach (var item in product.BundleProducts)
                {
                    var p1 = await _productRepository.GetByIdAsync(item.ProductId);
                    if (p1 is not {
                            BackorderModeId: BackorderMode.NoBackorders,
                            ManageInventoryMethodId: ManageInventoryMethod.ManageStock
                            or ManageInventoryMethod.ManageStockByAttributes
                        }) continue;
                    if (!await IsAvailable(p1, quantity * item.Quantity, attributes, warehouseId))
                        return false;
                }

                return true;
            default:
                return true;
        }
    }

    #endregion

    #region Inventory management methods

    /// <summary>
    ///     Adjust reserved inventory
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="quantityToChange">Quantity to increase or decrease</param>
    /// <param name="attributes">Attributes</param>
    /// <param name="warehouseId">Warehouse ident</param>
    public virtual async Task AdjustReserved(Product product, int quantityToChange,
        IList<CustomAttribute> attributes = null, string warehouseId = "")
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantityToChange == 0)
            return;

        await using var _ = await _resourceLock.Acquire(ProductKeys(product, attributes));

        //the passed product may come from a cache: only its id counts
        var storedProduct = await _productRepository.GetByIdAsync(product.Id);
        if (storedProduct == null)
            return;

        await AdjustReservedCore(storedProduct, quantityToChange, attributes, warehouseId);
    }

    public virtual async Task<bool> TryReserve(Product product, int quantity, IList<CustomAttribute> attributes,
        string warehouseId)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var _ = await _resourceLock.Acquire(ProductKeys(product, attributes));

        var storedProduct = await _productRepository.GetByIdAsync(product.Id);
        if (storedProduct == null || !await IsAvailable(storedProduct, quantity, attributes, warehouseId))
            return false;

        await AdjustReservedCore(storedProduct, -quantity, attributes, warehouseId);
        return true;
    }

    public virtual async Task CancelReservation(Product product, int quantity, IList<CustomAttribute> attributes,
        string warehouseId)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity == 0)
            return;

        await using var _ = await _resourceLock.Acquire(ProductKeys(product, attributes));

        var storedProduct = await _productRepository.GetByIdAsync(product.Id);
        if (storedProduct == null)
            return;

        await AdjustReservedCore(storedProduct, quantity, attributes, warehouseId, true);
    }

    /// <summary>
    ///     Adjusts the reserved inventory of a product read under the lock; never takes the lock itself
    /// </summary>
    /// <param name="restoreLowStock">The reservation is given back for an order that was never placed</param>
    private async Task AdjustReservedCore(Product product, int quantityToChange, IList<CustomAttribute> attributes,
        string warehouseId, bool restoreLowStock = false)
    {
        if (quantityToChange == 0)
            return;

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
        {
            var prevStockQuantity = _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId);

            //update stock quantity
            if (quantityToChange < 0)
                await ReserveInventory(product, quantityToChange, warehouseId);
            else
                await UnblockReservedInventory(product, quantityToChange, warehouseId);

            if (product.UseMultipleWarehouses)
            {
                product.StockQuantity = product.ProductWarehouseInventory.Sum(x => x.StockQuantity);
                product.ReservedQuantity = product.ProductWarehouseInventory.Sum(x => x.ReservedQuantity);

                await UpdateStockProduct(product);
            }

            //check if minimum quantity is reached
            if (quantityToChange < 0 && product.MinStockQuantity >=
                _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId))
                switch (product.LowStockActivityId)
                {
                    case LowStockActivity.DisableBuyButton:
                        product.DisableBuyButton = true;
                        product.LowStock = true;

                        await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                            UpdateBuilder<Product>.Create()
                                .Set(x => x.DisableBuyButton, product.DisableBuyButton)
                                .Set(x => x.LowStock, product.LowStock));

                        await _mediator.Publish(new ProductChangedEvent(product.Id));

                        //event notification
                        await _mediator.EntityUpdated(product);

                        break;
                    case LowStockActivity.Unpublish:
                        product.Published = false;
                        product.LowStock = true;

                        await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                            UpdateBuilder<Product>.Create()
                                .Set(x => x.Published, product.Published)
                                .Set(x => x.LowStock, product.LowStock));

                        await _mediator.Publish(new ProductChangedEvent(product.Id, listsChanged: true));

                        //event
                        await _mediator.Publish(new ProductUnPublishEvent(product));

                        //event notification
                        await _mediator.EntityUpdated(product);

                        break;
                }

            //qty is increased. product is out of stock (minimum stock quantity is reached again)? a reservation given
            //back for an order that was never placed undoes its own low-stock action, once above the minimum again
            if (_catalogSettings.PublishBackProductWhenCancellingOrders || (restoreLowStock &&
                    product.MinStockQuantity <
                    _stockQuantityService.GetTotalStockQuantity(product,
                        warehouseId: StockWarehouse(product, warehouseId))))
                if (quantityToChange > 0 && product.MinStockQuantity >= prevStockQuantity)
                    switch (product.LowStockActivityId)
                    {
                        case LowStockActivity.DisableBuyButton:
                            product.DisableBuyButton = false;
                            product.LowStock = product.MinStockQuantity <= prevStockQuantity;

                            await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                                UpdateBuilder<Product>.Create()
                                    .Set(x => x.DisableBuyButton, product.DisableBuyButton)
                                    .Set(x => x.LowStock, product.LowStock));

                            await _mediator.Publish(new ProductChangedEvent(product.Id));

                            //event notification
                            await _mediator.EntityUpdated(product);

                            break;
                        case LowStockActivity.Unpublish:
                            product.Published = true;
                            product.LowStock = product.MinStockQuantity < prevStockQuantity;

                            await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                                UpdateBuilder<Product>.Create()
                                    .Set(x => x.Published, product.Published)
                                    .Set(x => x.LowStock, product.LowStock));

                            await _mediator.Publish(new ProductChangedEvent(product.Id, listsChanged: true));

                            //event
                            await _mediator.Publish(new ProductPublishEvent(product));

                            //event notification
                            await _mediator.EntityUpdated(product);

                            break;
                    }

            //send email notification
            if (quantityToChange < 0 && _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId) <
                product.NotifyAdminForQuantityBelow)
                await _mediator.Send(new SendQuantityBelowStoreOwnerCommand {
                    Product = product
                });
        }

        if (attributes != null && product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
        {
            var combination = product.FindProductAttributeCombination(attributes);
            if (combination != null)
            {
                if (quantityToChange < 0)
                    await ReserveInventoryCombination(product, combination, quantityToChange, warehouseId);
                else
                    await UnblockReservedInventoryCombination(product, combination, quantityToChange, warehouseId);

                //send email notification
                if (quantityToChange < 0 && combination.StockQuantity < combination.NotifyAdminForQuantityBelow)
                    await _mediator.Send(new SendQuantityBelowStoreOwnerCommand {
                        Product = product,
                        ProductAttributeCombination = combination
                    });
            }
        }

        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            foreach (var item in product.BundleProducts)
            {
                var p1 = await _productRepository.GetByIdAsync(item.ProductId);
                if (p1 is {
                        ManageInventoryMethodId: ManageInventoryMethod.ManageStock
                        or ManageInventoryMethod.ManageStockByAttributes
                    }) await AdjustReservedCore(p1, quantityToChange * item.Quantity, attributes, warehouseId,
                    restoreLowStock);
            }

        //bundled products
        var attributeValues = product.ParseProductAttributeValues(attributes);
        foreach (var attributeValue in attributeValues)
        {
            if (attributeValue.AttributeValueTypeId != AttributeValueType.AssociatedToProduct) continue;
            //associated product (bundle)
            var associatedProduct = await _productRepository.GetByIdAsync(attributeValue.AssociatedProductId);
            if (associatedProduct != null)
                await AdjustReservedCore(associatedProduct, quantityToChange * attributeValue.Quantity, null,
                    warehouseId, restoreLowStock);
        }

        //event notification
        await _mediator.EntityUpdated(product);
    }

    /// <summary>
    ///     Reserve the given quantity in the warehouses.
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="quantity">Quantity, must be negative</param>
    /// <param name="warehouseId"></param>
    protected virtual async Task ReserveInventory(Product product, int quantity, string warehouseId)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity >= 0)
            throw new ArgumentException("Value must be negative.", nameof(quantity));

        var qty = -quantity;

        if (!product.UseMultipleWarehouses)
        {
            product.ReservedQuantity += qty;
            await UpdateStockProduct(product);
        }
        else
        {
            var pwi = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (pwi == null)
                return;

            pwi.ReservedQuantity += qty;
            if (pwi.ReservedQuantity < 0)
                pwi.ReservedQuantity = 0;

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductWarehouseInventory, z => z.Id == pwi.Id, pwi);
        }

        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        await _mediator.EntityUpdated(product);
    }


    /// <summary>
    ///     Reserve the given quantity in the warehouses.
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="combination">Combination</param>
    /// <param name="quantity">Quantity, must be negative</param>
    /// <param name="warehouseId">Warehouse ident</param>
    protected virtual async Task ReserveInventoryCombination(Product product, ProductAttributeCombination combination,
        int quantity, string warehouseId)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(combination);

        if (quantity >= 0)
            throw new ArgumentException("Value must be negative.", nameof(quantity));

        var qty = -quantity;

        if (!product.UseMultipleWarehouses)
        {
            combination.ReservedQuantity += qty;
            if (combination.ReservedQuantity < 0)
                combination.ReservedQuantity = 0;

            product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
            await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                UpdateBuilder<Product>.Create().Set(x => x.ReservedQuantity, product.ReservedQuantity));
        }
        else
        {
            var pwi = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (pwi == null)
                return;

            pwi.ReservedQuantity += qty;
            if (pwi.ReservedQuantity < 0)
                pwi.ReservedQuantity = 0;

            combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
            combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);

            product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
            await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                UpdateBuilder<Product>.Create().Set(x => x.ReservedQuantity, product.ReservedQuantity));
        }

        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        await _mediator.EntityUpdated(product);
    }

    /// <summary>
    ///     Unblocks the given quantity reserved items in the warehouses
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="quantity">Quantity, must be positive</param>
    /// <param name="warehouseId">Warehouse ident</param>
    protected virtual async Task UnblockReservedInventory(Product product, int quantity, string warehouseId)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity < 0)
            throw new ArgumentException("Value must be positive.", nameof(quantity));

        var qty = quantity;

        if (!product.UseMultipleWarehouses)
        {
            product.ReservedQuantity -= qty;
            await UpdateStockProduct(product);
        }
        else
        {
            if (!product.ProductWarehouseInventory.Any())
                return;

            var pwi = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (pwi == null)
                return;

            pwi.ReservedQuantity -= qty;
            if (pwi.ReservedQuantity < 0)
                pwi.ReservedQuantity = 0;

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductWarehouseInventory, z => z.Id == pwi.Id, pwi);
        }

        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        await _mediator.EntityUpdated(product);
    }

    /// <summary>
    ///     Unblocks the given quantity reserved items in the warehouses
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="combination">Combination</param>
    /// <param name="quantity">Quantity, must be positive</param>
    /// <param name="warehouseId">Warehouse ident</param>
    protected virtual async Task UnblockReservedInventoryCombination(Product product,
        ProductAttributeCombination combination, int quantity, string warehouseId)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (quantity < 0)
            throw new ArgumentException("Value must be positive.", nameof(quantity));

        if (!product.UseMultipleWarehouses)
        {
            combination.ReservedQuantity -= quantity;
            if (combination.ReservedQuantity < 0)
                combination.ReservedQuantity = 0;

            product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
            await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
                UpdateBuilder<Product>.Create().Set(x => x.ReservedQuantity, product.ReservedQuantity));
        }
        else
        {
            var pwi = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
            if (pwi == null)
                return;

            pwi.ReservedQuantity -= quantity;
            if (pwi.ReservedQuantity < 0)
                pwi.ReservedQuantity = 0;

            combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);

            await _productRepository.UpdateCollectionFieldItem(product.Id, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
        }

        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        await _mediator.EntityUpdated(product);
    }

    /// <summary>
    ///     Book the reserved quantity
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="shipmentItem">Shipment item</param>
    public virtual async Task BookReservedInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(shipmentItem);

        await using var _ = await _resourceLock.Acquire(ProductKeys(product, shipmentItem.Attributes));

        //the passed product may come from a cache: only its id counts
        var storedProduct = await _productRepository.GetByIdAsync(product.Id);
        if (storedProduct == null)
            return;

        await BookReservedInventoryCore(storedProduct, shipment, shipmentItem);
    }

    /// <summary>
    ///     Books the reserved quantity of a product read under the lock; never takes the lock itself
    /// </summary>
    private async Task BookReservedInventoryCore(Product product, Shipment shipment, ShipmentItem shipmentItem)
    {
        //standard manage stock 
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
            await ManageStockInventory(product, shipmentItem);
        //manage stock by attributes
        if (shipmentItem.Attributes != null &&
            product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            await ManageStockByAttributesInventory(product, shipmentItem);

        //manage stock by bundle products
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            await ManageStockByBundleProductsInventory(product, shipment, shipmentItem);

        //check if exist associated products
        if (shipmentItem.Attributes != null && shipmentItem.Attributes.Any())
            await ManageAttributesInventory(product, shipment, shipmentItem);

        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        await _mediator.EntityUpdated(product);

        //insert inventory journal
        if (product.ManageInventoryMethodId is ManageInventoryMethod.ManageStock
            or ManageInventoryMethod.ManageStockByAttributes)
            await InsertInventoryJournal(product, shipment, shipmentItem);
    }

    /// <summary>
    ///     Reverse booked inventory
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <param name="shipmentItem">Shipment item</param>
    /// <returns>Quantity reversed</returns>
    public virtual async Task ReverseBookedInventory(Shipment shipment, ShipmentItem shipmentItem)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        ArgumentNullException.ThrowIfNull(shipmentItem);

        var inventoryJournals = _inventoryJournalRepository.Table.Where(j => j.PositionId == shipmentItem.Id);

        foreach (var inventoryJournal in inventoryJournals)
        {
            await using var _ = await _resourceLock.Acquire([LockKey(inventoryJournal.ProductId)]);

            var product = await _productRepository.GetByIdAsync(inventoryJournal.ProductId);
            if (product == null)
                continue;

            await ReverseBookedInventory(product, inventoryJournal);

            await _mediator.Publish(new ProductChangedEvent(product.Id));

            //event notification
            await _mediator.EntityUpdated(product);
        }
    }


    public virtual async Task UpdateStockProduct(Product product, bool mediator = true)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (product.ReservedQuantity < 0)
            product.ReservedQuantity = 0;

        //update - one write, so the three stock fields never land out of step
        await _productRepository.UpdateOneAsync(x => x.Id == product.Id,
            UpdateBuilder<Product>.Create()
                .Set(x => x.StockQuantity, product.StockQuantity)
                .Set(x => x.ReservedQuantity, product.ReservedQuantity)
                .Set(x => x.LowStock,
                    (product.MinStockQuantity > 0 &&
                     product.MinStockQuantity >= product.StockQuantity - product.ReservedQuantity) ||
                    product.StockQuantity - product.ReservedQuantity <= 0));


        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        if (mediator)
            await _mediator.Publish(new UpdateStockEvent(product));

        //event notification
        await _mediator.EntityUpdated(product);
    }

    #endregion
}