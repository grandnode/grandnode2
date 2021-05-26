using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
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
    /// Product tag service
    /// </summary>
    public partial class ProductTagService : IProductTagService
    {
        #region Fields

        private readonly IRepository<ProductTag> _productTagRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="productTagRepository">Product tag repository</param>
        /// <param name="cacheBase">Cache manager</param>
        /// <param name="mediator">Mediator</param>
        public ProductTagService(IRepository<ProductTag> productTagRepository,
            IRepository<Product> productRepository,
            ICacheBase cacheBase,
            IMediator mediator
            )
        {
            _productTagRepository = productTagRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
            _productRepository = productRepository;
        }

        #endregion

        #region Nested classes

        private class ProductTagWithCount
        {
            public int ProductTagId { get; set; }
            public int ProductCount { get; set; }
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get product count for each of existing product tag
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Dictionary of "product tag ID : product count"</returns>
        private async Task<Dictionary<string, int>> GetProductCount(string storeId)
        {
            string key = string.Format(CacheKey.PRODUCTTAG_COUNT_KEY, storeId);
            return await _cacheBase.GetAsync(key, async () =>
             {
                 var query = from pt in _productTagRepository.Table
                             select pt;

                 var dictionary = new Dictionary<string, int>();
                 foreach (var item in query.ToList())
                     dictionary.Add(item.Id, item.Count);
                 return await Task.FromResult(dictionary);
             });
        }

        #endregion

        #region Methods


        /// <summary>
        /// Gets all product tags
        /// </summary>
        /// <returns>Product tags</returns>
        public virtual async Task<IList<ProductTag>> GetAllProductTags()
        {
            return await _cacheBase.GetAsync(CacheKey.PRODUCTTAG_ALL_KEY, async () =>
            {
                return await Task.FromResult(_productTagRepository.Table.ToList());
            });
        }

        /// <summary>
        /// Gets product tag
        /// </summary>
        /// <param name="productTagId">Product tag identifier</param>
        /// <returns>Product tag</returns>
        public virtual Task<ProductTag> GetProductTagById(string productTagId)
        {
            return _productTagRepository.GetByIdAsync(productTagId);
        }

        /// <summary>
        /// Gets product tag by name
        /// </summary>
        /// <param name="name">Product tag name</param>
        /// <returns>Product tag</returns>
        public virtual Task<ProductTag> GetProductTagByName(string name)
        {
            var query = from pt in _productTagRepository.Table
                        where pt.Name == name
                        select pt;

            return Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Gets product tag by sename
        /// </summary>
        /// <param name="sename">Product tag sename</param>
        /// <returns>Product tag</returns>
        public virtual Task<ProductTag> GetProductTagBySeName(string sename)
        {
            var query = from pt in _productTagRepository.Table
                        where pt.SeName == sename
                        select pt;
            return Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Inserts a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        public virtual async Task InsertProductTag(ProductTag productTag)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));

            await _productTagRepository.InsertAsync(productTag);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(productTag);
        }

        /// <summary>
        /// Inserts a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        public virtual async Task UpdateProductTag(ProductTag productTag)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));

            var previouse = await GetProductTagById(productTag.Id);

            await _productTagRepository.UpdateAsync(productTag);

            //update on products
            await _productRepository.UpdateToSet(x => x.ProductTags, previouse.Name, productTag.Name, true);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(productTag);
        }
        /// <summary>
        /// Delete a product tag
        /// </summary>
        /// <param name="productTag">Product tag</param>
        public virtual async Task DeleteProductTag(ProductTag productTag)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));

            //update product
            await _productRepository.Pull(string.Empty, x => x.ProductTags, productTag.Name, true);

            //delete tag
            await _productTagRepository.DeleteAsync(productTag);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(productTag);
        }

        /// <summary>
        /// Attach a tag to the product
        /// </summary>
        /// <param name="productTag">Product tag</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task AttachProductTag(ProductTag productTag, string productId)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));

            //assign to product
            await _productRepository.AddToSet(productId, x => x.ProductTags, productTag.Name);

            //update product tag
            await _productTagRepository.UpdateField(productTag.Id, x => x.Count, productTag.Count+1);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(productTag);
        }

        /// <summary>
        /// Detach a tag from the product
        /// </summary>
        /// <param name="productTag">Product Tag</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task DetachProductTag(ProductTag productTag, string productId)
        {
            if (productTag == null)
                throw new ArgumentNullException(nameof(productTag));


            await _productRepository.Pull(productId, x => x.ProductTags, productTag.Name);

            //update product tag
            await _productTagRepository.UpdateField(productTag.Id, x => x.Count, productTag.Count-1);

            //cache
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTTAG_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(productTag);
        }


        /// <summary>
        /// Get number of products
        /// </summary>
        /// <param name="productTagId">Product tag identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Number of products</returns>
        public virtual async Task<int> GetProductCount(string productTagId, string storeId)
        {
            var dictionary = await GetProductCount(storeId);
            if (dictionary.ContainsKey(productTagId))
                return dictionary[productTagId];

            return 0;
        }

        #endregion
    }
}
