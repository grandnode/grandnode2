using Grand.Business.Catalog.Commands.Models;
using Grand.Business.Catalog.Events.Models;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Data;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Products
{
    public class InventoryManageService : IInventoryManageService
    {
        #region Fields

        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<InventoryJournal> _inventoryJournalRepository;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public InventoryManageService(
            IRepository<Product> productRepository,
            IRepository<InventoryJournal> inventoryJournalRepository,
            IProductAttributeParser productAttributeParser,
            IStockQuantityService stockQuantityService,
            ICacheBase cacheBase,
            IMediator mediator,
            CatalogSettings catalogSettings)
        {
            _productRepository = productRepository;
            _inventoryJournalRepository = inventoryJournalRepository;
            _productAttributeParser = productAttributeParser;
            _stockQuantityService = stockQuantityService;
            _cacheBase = cacheBase;
            _mediator = mediator;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Utilities methods

        private async Task ManageStockInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
        {
            if (product.UseMultipleWarehouses)
            {
                var pwi = product.ProductWarehouseInventory.FirstOrDefault(pi => pi.WarehouseId == shipmentItem.WarehouseId);
                if (pwi == null)
                    return;

                pwi.ReservedQuantity -= shipmentItem.Quantity;
                pwi.StockQuantity -= shipmentItem.Quantity;
                if (pwi.ReservedQuantity < 0)
                    pwi.ReservedQuantity = 0;

                await _productRepository.UpdateToSet(product.Id, x => x.ProductWarehouseInventory, z => z.Id, pwi.Id, pwi);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

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
        private async Task ManageStockByAttributesInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
        {
            var combination = _productAttributeParser.FindProductAttributeCombination(product, shipmentItem.Attributes);
            if (combination == null)
                return;

            if (!product.UseMultipleWarehouses)
            {
                combination.ReservedQuantity -= shipmentItem.Quantity;
                combination.StockQuantity -= shipmentItem.Quantity;
                if (combination.ReservedQuantity < 0)
                    combination.ReservedQuantity = 0;

                await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);
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

                await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

            }
            product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
            product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);
            await UpdateStockProduct(product);
        }
        private async Task ManageStockByBundleProductsInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
        {
            foreach (var item in product.BundleProducts)
            {
                var p1 = await _productRepository.GetByIdAsync(item.ProductId);
                if (p1 != null && p1.Id != product.Id &&
                    p1.ManageInventoryMethodId != ManageInventoryMethod.DontManageStock)
                {
                    var _shipmentItem = new ShipmentItem() {
                        Id = shipmentItem.Id,
                        Attributes = shipmentItem.Attributes,
                        OrderItemId = shipmentItem.OrderItemId,
                        ProductId = shipmentItem.ProductId,
                        Quantity = shipmentItem.Quantity * item.Quantity,
                        WarehouseId = shipmentItem.WarehouseId
                    };
                    if (!await CheckExistsInventoryJournal(p1, _shipmentItem))
                    {
                        await BookReservedInventory(p1, shipment, _shipmentItem);
                    }
                }
            }
        }
        private async Task ManageAttributesInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
        {
            var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, shipmentItem.Attributes);
            foreach (var attributeValue in attributeValues)
            {
                if (attributeValue.AttributeValueTypeId == AttributeValueType.AssociatedToProduct)
                {
                    //associated product
                    var associatedProduct = await _productRepository.GetByIdAsync(attributeValue.AssociatedProductId);
                    if (associatedProduct != null
                         && associatedProduct.Id != product.Id
                         && associatedProduct.ManageInventoryMethodId != ManageInventoryMethod.DontManageStock)
                    {
                        if (!await CheckExistsInventoryJournal(associatedProduct, shipmentItem))
                        {
                            var _shipmentItem = new ShipmentItem() {
                                Id = shipmentItem.Id,
                                Attributes = shipmentItem.Attributes,
                                OrderItemId = shipmentItem.OrderItemId,
                                ProductId = shipmentItem.ProductId,
                                Quantity = shipmentItem.Quantity * attributeValue.Quantity,
                                WarehouseId = shipmentItem.WarehouseId
                            };
                            await BookReservedInventory(associatedProduct, shipment, _shipmentItem);
                        }
                    }
                }
            }
        }

        private async Task<bool> CheckExistsInventoryJournal(Product product, ShipmentItem shipmentItem)
        {
            var query = from j in _inventoryJournalRepository.Table
                        where j.ProductId == product.Id && j.PositionId == shipmentItem.Id
                        select j.Id;

            return await Task.FromResult(query.Any());
        }
        private async Task ReverseBookedInventory(Product product, InventoryJournal inventoryJournal)
        {
            //standard manage stock
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
            {
                if (product.UseMultipleWarehouses)
                {
                    var pwi = product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == inventoryJournal.WarehouseId);
                    if (pwi == null)
                        return;

                    pwi.StockQuantity += inventoryJournal.OutQty;
                    pwi.ReservedQuantity += inventoryJournal.OutQty;
                    if (pwi.ReservedQuantity < 0)
                        pwi.ReservedQuantity = 0;

                    await _productRepository.UpdateToSet(product.Id, x => x.ProductWarehouseInventory, z => z.Id, pwi.Id, pwi);
                    await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

                }
                else
                {
                    product.StockQuantity += inventoryJournal.OutQty;
                    product.ReservedQuantity += inventoryJournal.OutQty;
                    await UpdateStockProduct(product);
                }
            }

            //manage stock by attributes
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                var combination = _productAttributeParser.FindProductAttributeCombination(product, inventoryJournal.Attributes);
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

                    await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                    await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

                    await UpdateStockProduct(product);


                }
                else
                {
                    var pwi = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == inventoryJournal.WarehouseId);
                    if (pwi == null)
                        return;

                    pwi.StockQuantity += inventoryJournal.OutQty;
                    pwi.ReservedQuantity += inventoryJournal.OutQty;

                    if (pwi.ReservedQuantity < 0)
                        pwi.ReservedQuantity = 0;

                    combination.StockQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
                    combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.StockQuantity);
                    product.StockQuantity = product.ProductAttributeCombinations.Sum(x => x.StockQuantity);
                    product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

                    await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                    await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);
                    await UpdateStockProduct(product);

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

        #endregion

        #region Inventory management methods

        /// <summary>
        /// Adjust reserved inventory
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="quantityToChange">Quantity to increase or descrease</param>
        /// <param name="attributes">Attributes</param>
        public virtual async Task AdjustReserved(Product product, int quantityToChange, IList<CustomAttribute> attributes = null, string warehouseId = "")
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

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
                if (quantityToChange < 0 && product.MinStockQuantity >= _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId))
                {
                    switch (product.LowStockActivityId)
                    {
                        case LowStockActivity.DisableBuyButton:
                            product.DisableBuyButton = true;
                            product.LowStock = true;

                            await _productRepository.UpdateField(product.Id, x => x.DisableBuyButton, product.DisableBuyButton);
                            await _productRepository.UpdateField(product.Id, x => x.LowStock, product.LowStock);
                            await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);
                            //cache
                            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

                            //event notification
                            await _mediator.EntityUpdated(product);

                            break;
                        case LowStockActivity.Unpublish:
                            product.Published = false;
                            product.LowStock = true;

                            await _productRepository.UpdateField(product.Id, x => x.Published, product.Published);
                            await _productRepository.UpdateField(product.Id, x => x.LowStock, product.LowStock);
                            await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

                            //cache
                            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

                            //event
                            await _mediator.Publish(new ProductUnPublishEvent(product));

                            //event notification
                            await _mediator.EntityUpdated(product);

                            break;
                        default:
                            break;
                    }
                }
                //qty is increased. product is out of stock (minimum stock quantity is reached again)?
                if (_catalogSettings.PublishBackProductWhenCancellingOrders)
                {
                    var totalStock = prevStockQuantity;
                    if (quantityToChange > 0 && product.MinStockQuantity >= prevStockQuantity)
                    {
                        switch (product.LowStockActivityId)
                        {
                            case LowStockActivity.DisableBuyButton:
                                product.DisableBuyButton = false;
                                product.LowStock = product.MinStockQuantity <= totalStock;

                                await _productRepository.UpdateField(product.Id, x => x.DisableBuyButton, product.DisableBuyButton);
                                await _productRepository.UpdateField(product.Id, x => x.LowStock, product.LowStock);
                                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

                                //cache
                                await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

                                //event notification
                                await _mediator.EntityUpdated(product);

                                break;
                            case LowStockActivity.Unpublish:
                                product.Published = true;
                                product.LowStock = product.MinStockQuantity < totalStock;

                                await _productRepository.UpdateField(product.Id, x => x.Published, product.Published);
                                await _productRepository.UpdateField(product.Id, x => x.LowStock, product.LowStock);
                                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

                                //cache
                                await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

                                //event
                                await _mediator.Publish(new ProductPublishEvent(product));

                                //event notification
                                await _mediator.EntityUpdated(product);

                                break;
                            default:
                                break;
                        }
                    }
                }

                //send email notification
                if (quantityToChange < 0 && _stockQuantityService.GetTotalStockQuantity(product, warehouseId: warehouseId) < product.NotifyAdminForQuantityBelow)
                {
                    await _mediator.Send(new SendQuantityBelowStoreOwnerCommand() {
                        Product = product
                    });
                }
            }

            if (attributes != null && product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                var combination = _productAttributeParser.FindProductAttributeCombination(product, attributes);
                if (combination != null)
                {
                    if (quantityToChange < 0)
                        await ReserveInventoryCombination(product, combination, quantityToChange, warehouseId);
                    else
                        await UnblockReservedInventoryCombination(product, combination, quantityToChange, warehouseId);

                    //send email notification
                    if (quantityToChange < 0 && combination.StockQuantity < combination.NotifyAdminForQuantityBelow)
                    {
                        await _mediator.Send(new SendQuantityBelowStoreOwnerCommand() {
                            Product = product,
                            ProductAttributeCombination = combination
                        });
                    }
                }
            }

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            {
                foreach (var item in product.BundleProducts)
                {
                    var p1 = await _productRepository.GetByIdAsync(item.ProductId);
                    if (p1 != null && (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStock || p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes))
                    {
                        await AdjustReserved(p1, quantityToChange * item.Quantity, attributes, warehouseId);
                    }
                }
            }

            //bundled products
            var attributeValues = _productAttributeParser.ParseProductAttributeValues(product, attributes);
            foreach (var attributeValue in attributeValues)
            {
                if (attributeValue.AttributeValueTypeId == AttributeValueType.AssociatedToProduct)
                {
                    //associated product (bundle)
                    var associatedProduct = await _productRepository.GetByIdAsync(attributeValue.AssociatedProductId);
                    if (associatedProduct != null)
                    {
                        await AdjustReserved(associatedProduct, quantityToChange * attributeValue.Quantity, null, warehouseId);
                    }
                }
            }

            //event notification
            await _mediator.EntityUpdated(product);
        }

        /// <summary>
        /// Reserve the given quantity in the warehouses.
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="quantity">Quantity, must be negative</param>
        protected virtual async Task ReserveInventory(Product product, int quantity, string warehouseId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

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

                await _productRepository.UpdateToSet(product.Id, x => x.ProductWarehouseInventory, z => z.Id, pwi.Id, pwi);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

            }
            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            await _mediator.EntityUpdated(product);
        }


        /// <summary>
        /// Reserve the given quantity in the warehouses.
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="combination">Combination</param>
        /// <param name="quantity">Quantity, must be negative</param>
        protected virtual async Task ReserveInventoryCombination(Product product, ProductAttributeCombination combination, int quantity, string warehouseId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (combination == null)
                throw new ArgumentNullException(nameof(combination));

            if (quantity >= 0)
                throw new ArgumentException("Value must be negative.", nameof(quantity));

            var qty = -quantity;

            if (!product.UseMultipleWarehouses)
            {
                combination.ReservedQuantity += qty;
                if (combination.ReservedQuantity < 0)
                    combination.ReservedQuantity = 0;

                product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

                await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                await _productRepository.UpdateField(product.Id, x => x.ReservedQuantity, product.ReservedQuantity);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

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

                await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                await _productRepository.UpdateField(product.Id, x => x.ReservedQuantity, product.ReservedQuantity);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);
            }
            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            await _mediator.EntityUpdated(product);
        }

        /// <summary>
        /// Unblocks the given quantity reserved items in the warehouses
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="quantity">Quantity, must be positive</param>
        protected virtual async Task UnblockReservedInventory(Product product, int quantity, string warehouseId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

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

                await _productRepository.UpdateToSet(product.Id, x => x.ProductWarehouseInventory, z => z.Id, pwi.Id, pwi);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

            }
            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            await _mediator.EntityUpdated(product);
        }

        /// <summary>
        /// Unblocks the given quantity reserved items in the warehouses
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="quantity">Quantity, must be positive</param>
        protected virtual async Task UnblockReservedInventoryCombination(Product product, ProductAttributeCombination combination, int quantity, string warehouseId)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (quantity < 0)
                throw new ArgumentException("Value must be positive.", nameof(quantity));

            var qty = quantity;

            if (!product.UseMultipleWarehouses)
            {
                combination.ReservedQuantity -= qty;
                if (combination.ReservedQuantity < 0)
                    combination.ReservedQuantity = 0;

                product.ReservedQuantity = product.ProductAttributeCombinations.Sum(x => x.ReservedQuantity);

                await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                await _productRepository.UpdateField(product.Id, x => x.ReservedQuantity, product.ReservedQuantity);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

            }
            else
            {
                var pwi = combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == warehouseId);
                if (pwi == null)
                    return;

                pwi.ReservedQuantity -= qty;
                if (pwi.ReservedQuantity < 0)
                    pwi.ReservedQuantity = 0;

                combination.ReservedQuantity = combination.WarehouseInventory.Sum(x => x.ReservedQuantity);

                await _productRepository.UpdateToSet(product.Id, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);
                await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);

            }
            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            await _mediator.EntityUpdated(product);
        }

        /// <summary>
        /// Book the reserved quantity
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="shipment">Shipment</param>
        /// <param name="shipmentItem">Shipment item</param>
        public virtual async Task BookReservedInventory(Product product, Shipment shipment, ShipmentItem shipmentItem)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            if (shipmentItem == null)
                throw new ArgumentNullException(nameof(shipmentItem));

            //standard manage stock 
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
            {
                await ManageStockInventory(product, shipment, shipmentItem);
            }
            //manage stock by attributes
            if (shipmentItem.Attributes != null && product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            {
                await ManageStockByAttributesInventory(product, shipment, shipmentItem);
            }

            //manage stock by bundle products
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByBundleProducts)
            {
                await ManageStockByBundleProductsInventory(product, shipment, shipmentItem);
            }

            //check if exist associated products
            if (shipmentItem.Attributes != null && shipmentItem.Attributes.Any())
            {
                await ManageAttributesInventory(product, shipment, shipmentItem);
            }

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            await _mediator.EntityUpdated(product);

            //insert inventory journal
            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock || product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
                await InsertInventoryJournal(product, shipment, shipmentItem);

        }

        /// <summary>
        /// Reverse booked inventory
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="shipmentItem">Shipment item</param>
        /// <returns>Quantity reversed</returns>
        public virtual async Task ReverseBookedInventory(Shipment shipment, ShipmentItem shipmentItem)
        {

            if (shipment == null)
                throw new ArgumentNullException(nameof(shipment));

            if (shipmentItem == null)
                throw new ArgumentNullException(nameof(shipmentItem));

            var inventoryJournals = _inventoryJournalRepository.Table.Where(j => j.PositionId == shipmentItem.Id);

            foreach (var inventoryJournal in inventoryJournals)
            {
                var product = _productRepository.GetById(inventoryJournal.ProductId);
                if (product == null)
                    continue;

                await ReverseBookedInventory(product, inventoryJournal);

                //cache
                await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

                //event notification
                await _mediator.EntityUpdated(product);
            }

        }


        public virtual async Task UpdateStockProduct(Product product, bool mediator = true)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));
            if (product.ReservedQuantity < 0)
                product.ReservedQuantity = 0;

            //update
            await _productRepository.UpdateField(product.Id, x => x.StockQuantity, product.StockQuantity);
            await _productRepository.UpdateField(product.Id, x => x.ReservedQuantity, product.ReservedQuantity);
            await _productRepository.UpdateField(product.Id, x => x.LowStock, ((product.MinStockQuantity > 0 && product.MinStockQuantity >= product.StockQuantity - product.ReservedQuantity) || product.StockQuantity - product.ReservedQuantity <= 0));
            await _productRepository.UpdateField(product.Id, x => x.UpdatedOnUtc, DateTime.UtcNow);


            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, product.Id));

            //event notification
            if (mediator)
                await _mediator.Publish(new UpdateStockEvent(product));

            //event notification
            await _mediator.EntityUpdated(product);
        }


        #endregion
    }
}
