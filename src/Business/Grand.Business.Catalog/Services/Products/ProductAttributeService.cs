using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
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

            //delete from all product collections
            await _productRepository.PullFilter(string.Empty, x => x.ProductAttributeMappings, z => z.ProductAttributeId, productAttribute.Id, true);

            //delete from productAttribute collection
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

            await _productRepository.PullFilter(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMapping.Id);

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

            await _productRepository.AddToSet(productId, x => x.ProductAttributeMappings, productAttributeMapping);

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

            await _productRepository.UpdateToSet(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMapping.Id, productAttributeMapping);

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
                        await _productRepository.UpdateToSet(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMappingId, pavs);
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

            var p = await _productRepository.GetByIdAsync(productId);
            if (p == null)
                throw new ArgumentNullException(nameof(p));

            var pam = p.ProductAttributeMappings.FirstOrDefault(x => x.Id == productAttributeMappingId);
            if (pam == null)
                throw new ArgumentNullException(nameof(pam));

            pam.ProductAttributeValues.Add(productAttributeValue);
            await _productRepository.UpdateToSet(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMappingId, pam);

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

                        await _productRepository.UpdateToSet(productId, x => x.ProductAttributeMappings, z => z.Id, productAttributeMappingId, pavs);
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
            var pa = await Task.FromResult(_productAttributeRepository.Table.Where(x => x.Id == productAttributeId).FirstOrDefault());
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

            await _productRepository.PullFilter(productId, x => x.ProductAttributeCombinations, z => z.Id, combination.Id);
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

            await _productRepository.AddToSet(productId, x => x.ProductAttributeCombinations, combination);

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

            await _productRepository.UpdateToSet(productId, x => x.ProductAttributeCombinations, z => z.Id, combination.Id, combination);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(combination);
        }

        #endregion

        #endregion
    }
}
