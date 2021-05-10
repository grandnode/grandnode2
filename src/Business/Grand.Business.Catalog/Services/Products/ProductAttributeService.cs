using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Products
{
    /// <summary>
    /// Product attribute service
    /// </summary>
    public partial class ProductAttributeService : IProductAttributeService
    {
        #region Fields

        private readonly IRepository<ProductAttribute> _productAttributeRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;


        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="productAttributeRepository">Product attribute repository</param>
        /// <param name="productAttributeCombinationRepository">Product attribute combination repository</param>
        /// <param name="mediator">Mediator</param>
        public ProductAttributeService(ICacheBase cacheBase,
            IRepository<ProductAttribute> productAttributeRepository,
            IRepository<Product> productRepository,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _productAttributeRepository = productAttributeRepository;
            _productRepository = productRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        #region Product attributes

        /// <summary>
        /// Gets all product attributes
        /// </summary>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Product attributes</returns>
        public virtual async Task<IPagedList<ProductAttribute>> GetAllProductAttributes(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            string key = string.Format(CacheKey.PRODUCTATTRIBUTES_ALL_KEY, pageIndex, pageSize);
            return await _cacheBase.GetAsync(key, () =>
            {
                var query = from pa in _productAttributeRepository.Table
                            orderby pa.Name
                            select pa;
                return Task.FromResult(new PagedList<ProductAttribute>(query, pageIndex, pageSize));
            });
        }

        /// <summary>
        /// Gets a product attribute 
        /// </summary>
        /// <param name="productAttributeId">Product attribute identifier</param>
        /// <returns>Product attribute </returns>
        public virtual Task<ProductAttribute> GetProductAttributeById(string productAttributeId)
        {
            string key = string.Format(CacheKey.PRODUCTATTRIBUTES_BY_ID_KEY, productAttributeId);
            return _cacheBase.GetAsync(key, () => _productAttributeRepository.GetByIdAsync(productAttributeId));
        }

        /// <summary>
        /// Inserts a product attribute
        /// </summary>
        /// <param name="productAttribute">Product attribute</param>
        public virtual async Task InsertProductAttribute(ProductAttribute productAttribute)
        {
            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            await _productAttributeRepository.InsertAsync(productAttribute);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(productAttribute);
        }

        /// <summary>
        /// Updates the product attribute
        /// </summary>
        /// <param name="productAttribute">Product attribute</param>
        public virtual async Task UpdateProductAttribute(ProductAttribute productAttribute)
        {
            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            await _productAttributeRepository.UpdateAsync(productAttribute);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(productAttribute);
        }

        /// <summary>
        /// Deletes a product attribute
        /// </summary>
        /// <param name="productAttribute">Product attribute</param>
        public virtual async Task DeleteProductAttribute(ProductAttribute productAttribute)
        {
            if (productAttribute == null)
                throw new ArgumentNullException(nameof(productAttribute));

            var builder = Builders<Product>.Update;
            var updatefilter = builder.PullFilter(x => x.ProductAttributeMappings, y => y.ProductAttributeId == productAttribute.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument(), updatefilter);
            await _productAttributeRepository.DeleteAsync(productAttribute);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEMAPPINGS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTEVALUES_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTATTRIBUTECOMBINATIONS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(productAttribute);
        }

        #endregion

        #region Product attributes mappings

        /// <summary>
        /// Deletes a product attribute mapping
        /// </summary>
        /// <param name="productAttributeMapping">Product attribute mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteProductAttributeMapping(ProductAttributeMapping productAttributeMapping, string productId)
        {
            if (productAttributeMapping == null)
                throw new ArgumentNullException(nameof(productAttributeMapping));

            var updatebuilder = Builders<Product>.Update;
            var update = updatebuilder.PullFilter(p => p.ProductAttributeMappings, y => y.Id == productAttributeMapping.Id);
            await _productRepository.Collection.UpdateManyAsync(new BsonDocument("_id", productId), update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(productAttributeMapping);
        }

        /// <summary>
        /// Inserts a product attribute mapping
        /// </summary>
        /// <param name="productAttributeMapping">The product attribute mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertProductAttributeMapping(ProductAttributeMapping productAttributeMapping, string productId)
        {
            if (productAttributeMapping == null)
                throw new ArgumentNullException(nameof(productAttributeMapping));

            var updatebuilder = Builders<Product>.Update;
            var update = updatebuilder.AddToSet(p => p.ProductAttributeMappings, productAttributeMapping);
            await _productRepository.Collection.UpdateOneAsync(new BsonDocument("_id", productId), update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(productAttributeMapping);
        }

        /// <summary>
        /// Updates the product attribute mapping
        /// </summary>
        /// <param name="productAttributeMapping">The product attribute mapping</param>
        /// <param name="productId">Product ident</param>
        /// <param name="values">Update values</param>
        public virtual async Task UpdateProductAttributeMapping(ProductAttributeMapping productAttributeMapping, string productId, bool values = false)
        {
            if (productAttributeMapping == null)
                throw new ArgumentNullException(nameof(productAttributeMapping));

            var builder = Builders<Product>.Filter;
            var filter = builder.Eq(x => x.Id, productId);
            filter &= builder.ElemMatch(x => x.ProductAttributeMappings, y => y.Id == productAttributeMapping.Id);
            var update = Builders<Product>.Update
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ProductAttributeId, productAttributeMapping.ProductAttributeId)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).TextPrompt, productAttributeMapping.TextPrompt)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).IsRequired, productAttributeMapping.IsRequired)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ShowOnCatalogPage, productAttributeMapping.ShowOnCatalogPage)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).AttributeControlTypeId, productAttributeMapping.AttributeControlTypeId)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).DisplayOrder, productAttributeMapping.DisplayOrder)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).Combination, productAttributeMapping.Combination)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ValidationMinLength, productAttributeMapping.ValidationMinLength)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ValidationMaxLength, productAttributeMapping.ValidationMaxLength)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ValidationFileAllowedExtensions, productAttributeMapping.ValidationFileAllowedExtensions)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ValidationFileMaximumSize, productAttributeMapping.ValidationFileMaximumSize)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).DefaultValue, productAttributeMapping.DefaultValue)
                .Set(x => x.ProductAttributeMappings.ElementAt(-1).ConditionAttribute, productAttributeMapping.ConditionAttribute);

            if (values)
            {
                update = update.Set(x => x.ProductAttributeMappings.ElementAt(-1).ProductAttributeValues, productAttributeMapping.ProductAttributeValues);
            }

            await _productRepository.Collection.UpdateManyAsync(filter, update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(productAttributeMapping);
        }

        #endregion

        #region Product attribute values

        /// <summary>
        /// Deletes a product attribute value
        /// </summary>
        /// <param name="productAttributeValue">Product attribute value</param>
        /// <param name="productId">Product ident</param>
        /// <param name="productAttributeMappingId">Product attr mapping ident</param>
        public virtual async Task DeleteProductAttributeValue(ProductAttributeValue productAttributeValue, string productId, string productAttributeMappingId)
        {
            if (productAttributeValue == null)
                throw new ArgumentNullException(nameof(productAttributeValue));

            var filter = Builders<Product>.Filter.And(Builders<Product>.Filter.Eq(x => x.Id, productId),
            Builders<Product>.Filter.ElemMatch(x => x.ProductAttributeMappings, x => x.Id == productAttributeMappingId));

            var p = await _productRepository.GetByIdAsync(productId);
            if (p != null)
            {
                var pavs = p.ProductAttributeMappings.Where(x => x.Id == productAttributeMappingId).FirstOrDefault();
                if (pavs != null)
                {
                    var pav = pavs.ProductAttributeValues.Where(x => x.Id == productAttributeValue.Id).FirstOrDefault();
                    if (pav != null)
                    {
                        pavs.ProductAttributeValues.Remove(pav);
                        var update = Builders<Product>.Update.Set("ProductAttributeMappings.$", pavs);
                        await _productRepository.Collection.UpdateOneAsync(filter, update);
                    }
                }
            }

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(productAttributeValue);
        }


        /// <summary>
        /// Inserts a product attribute value
        /// </summary>
        /// <param name="productAttributeValue">The product attribute value</param>
        /// <param name="productId">Product ident</param>
        /// <param name="productAttributeMappingId">Product attr mapping ident</param>
        public virtual async Task InsertProductAttributeValue(ProductAttributeValue productAttributeValue, string productId, string productAttributeMappingId)
        {
            if (productAttributeValue == null)
                throw new ArgumentNullException(nameof(productAttributeValue));

            var updatebuilder = Builders<Product>.Update;
            var update = updatebuilder.AddToSet(p => p.ProductAttributeMappings.ElementAt(-1).ProductAttributeValues, productAttributeValue);

            var builder = Builders<Product>.Filter;
            var filter = builder.Eq(x => x.Id, productId);
            filter = filter & builder.Where(x => x.ProductAttributeMappings.Any(y => y.Id == productAttributeMappingId));

            await _productRepository.Collection.UpdateOneAsync(filter, update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(productAttributeValue);
        }

        /// <summary>
        /// Updates the product attribute value
        /// </summary>
        /// <param name="productAttributeValue">The product attribute value</param>
        /// <param name="productId">Product ident</param>
        /// <param name="productAttributeMappingId">Product attr mapping ident</param>
        public virtual async Task UpdateProductAttributeValue(ProductAttributeValue productAttributeValue, string productId, string productAttributeMappingId)
        {
            if (productAttributeValue == null)
                throw new ArgumentNullException(nameof(productAttributeValue));


            var filter = Builders<Product>.Filter.And(Builders<Product>.Filter.Eq(x => x.Id, productId),
            Builders<Product>.Filter.ElemMatch(x => x.ProductAttributeMappings, x => x.Id == productAttributeMappingId));

            var p = await _productRepository.GetByIdAsync(productId);
            if (p != null)
            {
                var pavs = p.ProductAttributeMappings.Where(x => x.Id == productAttributeMappingId).FirstOrDefault();
                if (pavs != null)
                {
                    var pav = pavs.ProductAttributeValues.Where(x => x.Id == productAttributeValue.Id).FirstOrDefault();
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

                        var update = Builders<Product>.Update.Set("ProductAttributeMappings.$", pavs);
                        await _productRepository.Collection.UpdateOneAsync(filter, update);
                    }
                }
            }

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(productAttributeValue);
        }

        #endregion

        #region Predefined product attribute values

        /// <summary>
        /// Gets predefined product attribute values by product attribute identifier
        /// </summary>
        /// <param name="productAttributeId">The product attribute identifier</param>
        /// <returns>Product attribute mapping collection</returns>
        public virtual async Task<IList<PredefinedProductAttributeValue>> GetPredefinedProductAttributeValues(string productAttributeId)
        {
            var builder = Builders<ProductAttribute>.Filter;
            var filter = FilterDefinition<ProductAttribute>.Empty;
            filter = filter & builder.Where(x => x.Id == productAttributeId);
            var pa = await _productAttributeRepository.Collection
                .Find(filter).FirstOrDefaultAsync();

            return pa.PredefinedProductAttributeValues.OrderBy(x => x.DisplayOrder).ToList();
        }


        #endregion

        #region Product attribute combinations

        /// <summary>
        /// Deletes a product attribute combination
        /// </summary>
        /// <param name="combination">Product attribute combination</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DeleteProductAttributeCombination(ProductAttributeCombination combination, string productId)
        {
            if (combination == null)
                throw new ArgumentNullException(nameof(combination));

            var updatebuilder = Builders<Product>.Update;

            var update = Builders<Product>.Update.PullFilter(p => p.ProductAttributeCombinations, Builders<ProductAttributeCombination>.Filter.Eq(per => per.Id, combination.Id));

            var result = await _productRepository.Collection.UpdateOneAsync(new BsonDocument("_id", productId), update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(combination);
        }

        /// <summary>
        /// Inserts a product attribute combination
        /// </summary>
        /// <param name="combination">Product attribute combination</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertProductAttributeCombination(ProductAttributeCombination combination, string productId)
        {
            if (combination == null)
                throw new ArgumentNullException(nameof(combination));

            var updatebuilder = Builders<Product>.Update;
            var update = updatebuilder.AddToSet(p => p.ProductAttributeCombinations, combination);
            await _productRepository.Collection.UpdateOneAsync(new BsonDocument("_id", productId), update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(combination);
        }

        /// <summary>
        /// Updates a product attribute combination
        /// </summary>
        /// <param name="combination">Product attribute combination</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task UpdateProductAttributeCombination(ProductAttributeCombination combination, string productId)
        {
            if (combination == null)
                throw new ArgumentNullException(nameof(combination));

            var builder = Builders<Product>.Filter;
            var filter = builder.Eq(x => x.Id, productId);
            filter &= builder.ElemMatch(x => x.ProductAttributeCombinations, y => y.Id == combination.Id);
            var update = Builders<Product>.Update
                .Set("ProductAttributeCombinations.$.StockQuantity", combination.StockQuantity)
                .Set("ProductAttributeCombinations.$.ReservedQuantity", combination.ReservedQuantity)
                .Set("ProductAttributeCombinations.$.AllowOutOfStockOrders", combination.AllowOutOfStockOrders)
                .Set("ProductAttributeCombinations.$.Sku", combination.Sku)
                .Set("ProductAttributeCombinations.$.Text", combination.Text)
                .Set("ProductAttributeCombinations.$.Mpn", combination.Mpn)
                .Set("ProductAttributeCombinations.$.Gtin", combination.Gtin)
                .Set("ProductAttributeCombinations.$.OverriddenPrice", combination.OverriddenPrice)
                .Set("ProductAttributeCombinations.$.NotifyAdminForQuantityBelow", combination.NotifyAdminForQuantityBelow)
                .Set("ProductAttributeCombinations.$.WarehouseInventory", combination.WarehouseInventory)
                .Set("ProductAttributeCombinations.$.PictureId", combination.PictureId)
                .Set("ProductAttributeCombinations.$.TierPrices", combination.TierPrices);

            await _productRepository.Collection.UpdateManyAsync(filter, update);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(combination);
        }

        #endregion

        #endregion
    }
}
