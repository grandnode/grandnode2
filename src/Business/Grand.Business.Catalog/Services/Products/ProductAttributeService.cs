using Grand.Business.Catalog.Services.Inventory;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Extensions;
using Grand.Mediator;

namespace Grand.Business.Catalog.Services.Products;

/// <summary>
///     Product attribute service
/// </summary>
public class ProductAttributeService : IProductAttributeService
{
    #region Ctor

    /// <summary>
    ///     Ctor
    /// </summary>
    /// <param name="cacheBase">Cache manager</param>
    /// <param name="productAttributeRepository">Product attribute repository</param>
    /// <param name="productRepository">Product repository</param>
    /// <param name="mediator">Mediator</param>
    public ProductAttributeService(ICacheBase cacheBase,
        IRepository<ProductAttribute> productAttributeRepository,
        IRepository<Product> productRepository,
        IMediator mediator,
        IResourceLock resourceLock,
        StockLedger stockLedger)
    {
        _cacheBase = cacheBase;
        _productAttributeRepository = productAttributeRepository;
        _productRepository = productRepository;
        _mediator = mediator;
        _resourceLock = resourceLock;
        _stockLedger = stockLedger;
    }

    #endregion

    #region Fields

    private readonly IRepository<ProductAttribute> _productAttributeRepository;
    private readonly IRepository<Product> _productRepository;
    private readonly IMediator _mediator;
    private readonly IResourceLock _resourceLock;
    private readonly StockLedger _stockLedger;
    private readonly ICacheBase _cacheBase;
    
    #endregion

    #region Methods

    #region Product attributes

    /// <summary>
    ///     Gets all product attributes
    /// </summary>
    /// <param name="storeId">Store ident</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Product attributes</returns>
    public virtual async Task<IPagedList<ProductAttribute>> GetAllProductAttributes(string storeId = "", int pageIndex = 0,
        int pageSize = int.MaxValue)
    {
        var key = string.Format(CacheKey.PRODUCTATTRIBUTES_ALL_KEY, storeId, pageIndex, pageSize);
        return await _cacheBase.GetAsync(key, async () =>
        {
            var query = from pa in _productAttributeRepository.Table
                select pa;

            if (!string.IsNullOrEmpty(storeId))
                //Limited to stores rules
                query = from p in query
                        where !p.LimitedToStores || p.Stores.Contains(storeId)
                        select p;

            query = query.OrderBy(pa => pa.Name);

            return await _productAttributeRepository.PagedAsync(query, pageIndex, pageSize);
        });

        
    }

    /// <summary>
    ///     Gets a product attribute
    /// </summary>
    /// <param name="productAttributeId">Product attribute identifier</param>
    /// <returns>Product attribute </returns>
    public virtual Task<ProductAttribute> GetProductAttributeById(string productAttributeId)
    {
        var key = string.Format(CacheKey.PRODUCTATTRIBUTES_BY_ID_KEY, productAttributeId);
        return _cacheBase.GetAsync(key, () => _productAttributeRepository.GetByIdAsync(productAttributeId));
    }

    /// <summary>
    ///     Inserts a product attribute
    /// </summary>
    /// <param name="productAttribute">Product attribute</param>
    public virtual async Task InsertProductAttribute(ProductAttribute productAttribute)
    {
        ArgumentNullException.ThrowIfNull(productAttribute);

        await _productAttributeRepository.InsertAsync(productAttribute);

        //event notification
        await _mediator.EntityInserted(productAttribute);
    }

    /// <summary>
    ///     Updates the product attribute
    /// </summary>
    /// <param name="productAttribute">Product attribute</param>
    public virtual async Task UpdateProductAttribute(ProductAttribute productAttribute)
    {
        ArgumentNullException.ThrowIfNull(productAttribute);

        await _productAttributeRepository.UpdateAsync(productAttribute);

        //event notification
        await _mediator.EntityUpdated(productAttribute);
    }

    /// <summary>
    ///     Deletes a product attribute
    /// </summary>
    /// <param name="productAttribute">Product attribute</param>
    public virtual async Task DeleteProductAttribute(ProductAttribute productAttribute)
    {
        ArgumentNullException.ThrowIfNull(productAttribute);

        //delete from all product collections
        await _productRepository.RemoveCollectionFieldItem(string.Empty, x => x.ProductAttributeMappings, z => z.ProductAttributeId == productAttribute.Id);

        //delete from productAttribute collection
        await _productAttributeRepository.DeleteAsync(productAttribute);

        //event notification
        await _mediator.EntityDeleted(productAttribute);
    }

    #endregion

    #region Product attributes mappings

    /// <summary>
    ///     Deletes a product attribute mapping
    /// </summary>
    /// <param name="productAttributeMapping">Product attribute mapping</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task DeleteProductAttributeMapping(ProductAttributeMapping productAttributeMapping,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(productAttributeMapping);

        await _productRepository.RemoveCollectionFieldItem(productId, x => x.ProductAttributeMappings, z => z.Id == productAttributeMapping.Id);

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityDeleted(productAttributeMapping);
    }

    /// <summary>
    ///     Inserts a product attribute mapping
    /// </summary>
    /// <param name="productAttributeMapping">The product attribute mapping</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task InsertProductAttributeMapping(ProductAttributeMapping productAttributeMapping,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(productAttributeMapping);

        await _productRepository.AddToCollectionField(productId, x => x.ProductAttributeMappings, productAttributeMapping);

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityInserted(productAttributeMapping);
    }

    /// <summary>
    ///     Updates the product attribute mapping
    /// </summary>
    /// <param name="productAttributeMapping">The product attribute mapping</param>
    /// <param name="productId">Product ident</param>
    /// <param name="values">Update values</param>
    public virtual async Task UpdateProductAttributeMapping(ProductAttributeMapping productAttributeMapping,
        string productId, bool values = false)
    {
        ArgumentNullException.ThrowIfNull(productAttributeMapping);

        await _productRepository.UpdateCollectionFieldItem(productId, x => x.ProductAttributeMappings, z => z.Id == productAttributeMapping.Id, productAttributeMapping);

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityUpdated(productAttributeMapping);
    }

    #endregion

    #region Product attribute values

    /// <summary>
    ///     Deletes a product attribute value
    /// </summary>
    /// <param name="productAttributeValue">Product attribute value</param>
    /// <param name="productId">Product ident</param>
    /// <param name="productAttributeMappingId">Product attr mapping ident</param>
    public virtual async Task DeleteProductAttributeValue(ProductAttributeValue productAttributeValue, string productId,
        string productAttributeMappingId)
    {
        ArgumentNullException.ThrowIfNull(productAttributeValue);

        var p = await _productRepository.GetByIdAsync(productId);
        if (p != null)
        {
            var pavs = p.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
            var pav = pavs?.ProductAttributeValues.FirstOrDefault(x => x.Id == productAttributeValue.Id);
            if (pav != null)
            {
                pavs.ProductAttributeValues.Remove(pav);
                await _productRepository.UpdateCollectionFieldItem(productId, x => x.ProductAttributeMappings, z => z.Id == productAttributeMappingId, pavs);
            }
        }

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityDeleted(productAttributeValue);
    }


    /// <summary>
    ///     Inserts a product attribute value
    /// </summary>
    /// <param name="productAttributeValue">The product attribute value</param>
    /// <param name="productId">Product ident</param>
    /// <param name="productAttributeMappingId">Product attr mapping ident</param>
    public virtual async Task InsertProductAttributeValue(ProductAttributeValue productAttributeValue, string productId,
        string productAttributeMappingId)
    {
        ArgumentNullException.ThrowIfNull(productAttributeValue);

        var p = await _productRepository.GetByIdAsync(productId);
        ArgumentNullException.ThrowIfNull(p);

        var pam = p.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        ArgumentNullException.ThrowIfNull(pam);

        pam.ProductAttributeValues.Add(productAttributeValue);
        await _productRepository.UpdateCollectionFieldItem(productId, x => x.ProductAttributeMappings, z => z.Id == productAttributeMappingId, pam);

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityInserted(productAttributeValue);
    }

    /// <summary>
    ///     Updates the product attribute value
    /// </summary>
    /// <param name="productAttributeValue">The product attribute value</param>
    /// <param name="productId">Product ident</param>
    /// <param name="productAttributeMappingId">Product attr mapping ident</param>
    public virtual async Task UpdateProductAttributeValue(ProductAttributeValue productAttributeValue, string productId,
        string productAttributeMappingId)
    {
        ArgumentNullException.ThrowIfNull(productAttributeValue);

        var p = await _productRepository.GetByIdAsync(productId);
        var pavs = p?.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
        var pav = pavs?.ProductAttributeValues.FirstOrDefault(x => x.Id == productAttributeValue.Id);
        if (pav != null)
        {
            pav.AttributeValueTypeId = productAttributeValue.AttributeValueTypeId;
            pav.AssociatedProductId = productAttributeValue.AssociatedProductId;
            pav.Name = productAttributeValue.Name;
            pav.ColorSquaresRgb = productAttributeValue.ColorSquaresRgb;
            pav.ImageSquaresPictureId = productAttributeValue.ImageSquaresPictureId;
            pav.PriceAdjustment = productAttributeValue.PriceAdjustment;
            pav.WeightAdjustment = productAttributeValue.WeightAdjustment;
            pav.Cost = productAttributeValue.Cost;
            pav.Quantity = productAttributeValue.Quantity;
            pav.IsPreSelected = productAttributeValue.IsPreSelected;
            pav.DisplayOrder = productAttributeValue.DisplayOrder;
            pav.PictureId = productAttributeValue.PictureId;
            pav.Locales = productAttributeValue.Locales;

            await _productRepository.UpdateCollectionFieldItem(productId, x => x.ProductAttributeMappings, z => z.Id == productAttributeMappingId, pavs);
        }

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityUpdated(productAttributeValue);
    }

    #endregion

    #region Product attribute combinations

    /// <summary>
    ///     Deletes a product attribute combination
    /// </summary>
    /// <param name="combination">Product attribute combination</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task DeleteProductAttributeCombination(ProductAttributeCombination combination,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(combination);

        await using (await _resourceLock.Acquire([ProductStockLock.Key(productId)]))
        {
            var product = await _productRepository.GetByIdAsync(productId);
            var stored = product?.ProductAttributeCombinations.FirstOrDefault(x => x.Id == combination.Id);
            if (product?.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes && stored != null)
            {
                //the stock the combination holds leaves the journal first, so the product's history keeps adding up
                var held = product.UseMultipleWarehouses
                    ? stored.WarehouseInventory.Select(x => (x.WarehouseId, x.StockQuantity))
                    : [("", stored.StockQuantity)];
                var changes = held.Where(x => x.StockQuantity != 0)
                    .Select(x => new StockChange(stored.Attributes, x.WarehouseId, -x.StockQuantity, false,
                        InventoryMovementType.Adjustment,
                        new StockSource(nameof(Product), productId, Comment: "Attribute combination removed")))
                    .ToList();
                if (changes.Count > 0)
                    await _stockLedger.Apply(product, changes);

                product.ProductAttributeCombinations.Remove(stored);
                await _stockLedger.RecomputeSums(product);
            }
            else
            {
                await _productRepository.RemoveCollectionFieldItem(productId, x => x.ProductAttributeCombinations, z => z.Id == combination.Id);
            }
        }

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityDeleted(combination);
    }

    /// <summary>
    ///     Inserts a product attribute combination
    /// </summary>
    /// <param name="combination">Product attribute combination</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task InsertProductAttributeCombination(ProductAttributeCombination combination,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(combination);

        //a new combination starts empty: its stock changes only through the stock ledger
        combination.StockQuantity = 0;
        combination.ReservedQuantity = 0;
        foreach (var row in combination.WarehouseInventory)
        {
            row.StockQuantity = 0;
            row.ReservedQuantity = 0;
        }

        await using (await _resourceLock.Acquire([ProductStockLock.Key(productId)]))
            await _productRepository.AddToCollectionField(productId, x => x.ProductAttributeCombinations, combination);

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityInserted(combination);
    }

    /// <summary>
    ///     Updates a product attribute combination
    /// </summary>
    /// <param name="combination">Product attribute combination</param>
    /// <param name="productId">Product ident</param>
    public virtual async Task UpdateProductAttributeCombination(ProductAttributeCombination combination,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(combination);

        await using (await _resourceLock.Acquire([ProductStockLock.Key(productId)]))
        {
            //the stored stock stays: it changes only through the stock ledger
            var stored = (await _productRepository.GetByIdAsync(productId))?.ProductAttributeCombinations
                .FirstOrDefault(x => x.Id == combination.Id);
            if (stored != null)
            {
                combination.StockQuantity = stored.StockQuantity;
                combination.ReservedQuantity = stored.ReservedQuantity;

                //the passed rows say which warehouses are used: a new one starts empty, and a stored one that is
                //left out goes only when it holds nothing
                var usedWarehouses = combination.WarehouseInventory.Select(x => x.WarehouseId).ToList();
                combination.WarehouseInventory.Clear();
                foreach (var row in stored.WarehouseInventory.Where(x =>
                             usedWarehouses.Contains(x.WarehouseId) || x.StockQuantity != 0 || x.ReservedQuantity != 0))
                    combination.WarehouseInventory.Add(row);
                foreach (var warehouseId in usedWarehouses.Except(stored.WarehouseInventory.Select(x => x.WarehouseId)))
                    combination.WarehouseInventory.Add(new ProductCombinationWarehouseInventory
                        { WarehouseId = warehouseId });
            }

            await _productRepository.UpdateCollectionFieldItem(productId, x => x.ProductAttributeCombinations, z => z.Id == combination.Id, combination);
        }

        await _mediator.Publish(new ProductChangedEvent(productId));

        //event notification
        await _mediator.EntityUpdated(combination);
    }

    #endregion

    #endregion
}