using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

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
        IResourceLock resourceLock,
        StockSideEffects stockSideEffects,
        StockLedger stockLedger,
        IRepository<Shipment> shipmentRepository,
        ILogger<InventoryManageService> logger)
    {
        _shipmentRepository = shipmentRepository;
        _logger = logger;
        _productRepository = productRepository;
        _inventoryJournalRepository = inventoryJournalRepository;
        _stockQuantityService = stockQuantityService;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
        _resourceLock = resourceLock;
        _stockSideEffects = stockSideEffects;
        _stockLedger = stockLedger;
    }

    #endregion

    #region Fields

    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<InventoryJournal> _inventoryJournalRepository;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly IMediator _mediator;
    private readonly CatalogSettings _catalogSettings;
    private readonly IResourceLock _resourceLock;
    private readonly StockSideEffects _stockSideEffects;
    private readonly StockLedger _stockLedger;
    private readonly IRepository<Shipment> _shipmentRepository;
    private readonly ILogger<InventoryManageService> _logger;

    #endregion

    #region Utilities methods

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

    /// <summary>
    ///     The journal entries of a shipment item: its bookings and their reversals, for every product it touched
    /// </summary>
    private async Task<IList<InventoryJournal>> ShipmentEntries(string positionId, string productId = null)
    {
        var query = _inventoryJournalRepository.Table.Where(j =>
            j.PositionId == positionId && j.ObjectType == nameof(Shipment));
        if (productId != null)
            query = query.Where(j => j.ProductId == productId);

        return await _inventoryJournalRepository.ToListAsync(query);
    }

    /// <summary>
    ///     The quantity a shipment item has booked out of the product and not reversed. A booking written before every
    ///     change was journaled counts too: the upgrade marked it legacy, and before the upgrade it reads as type 0
    /// </summary>
    private static int NetShipped(IEnumerable<InventoryJournal> entries)
    {
        return entries.Sum(x => x.MovementTypeId switch {
            InventoryMovementType.Shipment or InventoryMovementType.Legacy
                or InventoryMovementType.OpeningBalance => x.OutQty,
            InventoryMovementType.ShipmentReversal => -x.InQty,
            _ => 0
        });
    }

    /// <summary>
    ///     The serial numbers a shipment item's entries shipped and did not give back
    /// </summary>
    private static List<string> ShippedNumbers(IEnumerable<InventoryJournal> entries)
    {
        var list = entries.ToList();
        var returned = list.Where(x => x.MovementTypeId == InventoryMovementType.ShipmentReversal)
            .SelectMany(x => x.SerialNumbers ?? []).Select(UnitNumbers.Key).ToHashSet();
        return list.Where(x => x.MovementTypeId == InventoryMovementType.Shipment)
            .SelectMany(x => x.SerialNumbers ?? []).Where(x => !returned.Contains(UnitNumbers.Key(x))).ToList();
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
        return ProductStockLock.Key(productId);
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
        if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByBundleProducts)
            return _stockQuantityService.CheckStock(product, quantity, attributes, StockWarehouse(product, warehouseId))
                .Covered;

        foreach (var item in product.BundleProducts)
        {
            //the cart checks a bundle's product only when it does not allow backorders
            var p1 = await _productRepository.GetByIdAsync(item.ProductId);
            if (p1 is not { BackorderModeId: BackorderMode.NoBackorders }) continue;
            if (!_stockQuantityService.CheckStock(p1, quantity * item.Quantity, attributes,
                    StockWarehouse(p1, warehouseId)).Covered)
                return false;
        }

        return true;
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
                await _stockSideEffects.LowStockReached(product);

            //qty is increased. product is out of stock (minimum stock quantity is reached again)? a reservation given
            //back for an order that was never placed undoes its own low-stock action, once above the minimum again
            if (_catalogSettings.PublishBackProductWhenCancellingOrders || (restoreLowStock &&
                    product.MinStockQuantity <
                    _stockQuantityService.GetTotalStockQuantity(product,
                        warehouseId: StockWarehouse(product, warehouseId))))
                if (quantityToChange > 0 && product.MinStockQuantity >= prevStockQuantity)
                    await _stockSideEffects.LowStockLifted(product, prevStockQuantity);

            //send email notification
            if (quantityToChange < 0 && _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId) <
                product.NotifyAdminForQuantityBelow)
                await _stockSideEffects.QuantityBelow(product);
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
                    await _stockSideEffects.QuantityBelow(product, combination);
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
        //standard manage stock, or manage stock by attributes; once per shipment item, until it is reversed
        if ((product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock ||
             (shipmentItem.Attributes != null &&
              product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)) &&
            NetShipped(await ShipmentEntries(shipmentItem.Id, product.Id)) <= 0)
        {
            var errors = await _stockLedger.Apply(product, ShipmentChanges(product, shipment, shipmentItem));
            if (errors.Count > 0 && InventoryTracking.Tracked(product))
                await UnitsNotBooked(product, shipment, shipmentItem, errors);
        }

        //manage stock by bundle products
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            await ManageStockByBundleProductsInventory(product, shipment, shipmentItem);

        //check if exist associated products
        if (shipmentItem.Attributes != null && shipmentItem.Attributes.Any())
            await ManageAttributesInventory(product, shipment, shipmentItem);

        await _mediator.Publish(new ProductChangedEvent(product.Id));

        //event notification
        await _mediator.EntityUpdated(product);
    }

    /// <summary>
    ///     What a shipment item books: its quantity, with the batches and serial numbers it names for a product
    ///     tracking them; the quantity no batch names comes from the stock without one
    /// </summary>
    private static List<StockChange> ShipmentChanges(Product product, Shipment shipment, ShipmentItem shipmentItem)
    {
        StockChange Change(int quantity) => new(shipmentItem.Attributes ?? [], shipmentItem.WarehouseId, -quantity,
            false, InventoryMovementType.Shipment, ShipmentSource(shipment, shipmentItem));

        if (!InventoryTracking.Tracked(product))
            return [Change(shipmentItem.Quantity)];
        if (product.InventoryTrackingModeId == InventoryTrackingMode.SerialNumber)
            return [Change(shipmentItem.Quantity) with { SerialNumbers = (shipmentItem.SerialNumbers ?? []).ToList() }];

        var changes = (shipmentItem.Batches ?? []).Where(x => x.Quantity > 0)
            .GroupBy(x => UnitNumbers.Key(x.Number))
            .Select(x => Change(x.Sum(y => y.Quantity)) with { BatchNumber = x.First().Number })
            .ToList();
        var withoutBatch = shipmentItem.Quantity + changes.Sum(x => x.Quantity);
        if (withoutBatch > 0)
            changes.Add(Change(withoutBatch));
        return changes;
    }

    /// <summary>
    ///     Another shipment took a unit this one named between its check and its booking: the shipment is stored, so
    ///     its item is marked, and fixed by deleting and adding the shipment again
    /// </summary>
    private async Task UnitsNotBooked(Product product, Shipment shipment, ShipmentItem shipmentItem,
        IList<string> errors)
    {
        _logger.LogWarning("Shipment {ShipmentNumber}: the units of product {ProductId} were not booked: {Errors}",
            shipment.ShipmentNumber, product.Id, string.Join(", ", errors));

        //a bundle's product books under its item's id: only the item itself is marked
        if (shipmentItem.ProductId != product.Id)
            return;

        shipmentItem.UnitsNotBooked = true;
        await _shipmentRepository.UpdateCollectionFieldItem(shipment.Id, x => x.ShipmentItems,
            z => z.Id == shipmentItem.Id, shipmentItem);
    }

    private static StockSource ShipmentSource(Shipment shipment, ShipmentItem shipmentItem)
    {
        return new StockSource(nameof(Shipment), shipment.Id, shipmentItem.Id, shipment.ShipmentNumber.ToString(),
            Comment: $"Shipment - {shipment.TrackingNumber}");
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

        foreach (var productEntries in (await ShipmentEntries(shipmentItem.Id)).GroupBy(x => x.ProductId))
        {
            await using var _ = await _resourceLock.Acquire([LockKey(productEntries.Key)]);

            var product = await _productRepository.GetByIdAsync(productEntries.Key);
            if (product == null)
                continue;

            //read again under the lock: a reversal running alongside must not give the stock back twice
            //the same units come back: each batch its quantity, and the serial numbers not given back yet
            var changes = (await ShipmentEntries(shipmentItem.Id, product.Id))
                .GroupBy(x => (x.WarehouseId ?? "", x.CombinationId ?? "", UnitNumbers.Key(x.BatchNumber)))
                .Select(bucket => (Entry: bucket.First(), Quantity: NetShipped(bucket), SerialNumbers: ShippedNumbers(bucket)))
                .Where(x => x.Quantity > 0)
                .Select(x => new StockChange(x.Entry.Attributes ?? [], x.Entry.WarehouseId, x.Quantity, false,
                    InventoryMovementType.ShipmentReversal, ShipmentSource(shipment, shipmentItem)) {
                    BatchNumber = x.Entry.BatchNumber, SerialNumbers = x.SerialNumbers
                })
                .ToList();
            if (changes.Count == 0)
                continue;

            await _stockLedger.Apply(product, changes);

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