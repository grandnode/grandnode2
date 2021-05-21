using Grand.Business.Catalog.Interfaces.Collections;
using Grand.Business.Catalog.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Collections
{
    public class ProductCollectionService : IProductCollectionService
    {
        #region Fields

        private readonly IRepository<Product> _productRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly ICacheBase _cacheBase;

        #endregion

        #region Ctor

        public ProductCollectionService(ICacheBase cacheBase,
            IRepository<Product> productRepository,
            IWorkContext workContext,
            IMediator mediator)
        {
            _cacheBase = cacheBase;
            _productRepository = productRepository;
            _workContext = workContext;
            _mediator = mediator;
        }
        #endregion

        /// <summary>
        /// Gets product collection by collection id
        /// </summary>
        /// <param name="collectionId">Collection id</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value that indicates if it should shows hidden records</param>
        /// <returns>Product collection collection</returns>
        public virtual async Task<IPagedList<ProductsCollection>> GetProductCollectionsByCollectionId(string collectionId, string storeId,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            string key = string.Format(CacheKey.PRODUCTCOLLECTIONS_ALLBYCOLLECTIONID_KEY, showHidden, collectionId, pageIndex, pageSize, _workContext.CurrentCustomer.Id, storeId);
            return await _cacheBase.GetAsync(key, () =>
            {
                var query = _productRepository.Table.Where(x => x.ProductCollections.Any(y => y.CollectionId == collectionId));

                if (!showHidden && (!CommonHelper.IgnoreAcl || !CommonHelper.IgnoreStoreLimitations))
                {
                    if (!CommonHelper.IgnoreAcl)
                    {
                        //ACL (access control list)
                        var allowedCustomerGroupsIds = _workContext.CurrentCustomer.GetCustomerGroupIds();
                        query = from p in query
                                where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x))
                                select p;
                    }
                    if (!CommonHelper.IgnoreStoreLimitations && !string.IsNullOrEmpty(storeId))
                    {
                        //Store acl
                        query = from p in query
                                where !p.LimitedToStores || p.Stores.Contains(storeId)
                                select p;

                    }

                }

                var query_ProductCollection = from prod in query
                                              from pm in prod.ProductCollections
                                              select new ProductsCollection {
                                                  Id = pm.Id,
                                                  ProductId = prod.Id,
                                                  DisplayOrder = pm.DisplayOrder,
                                                  IsFeaturedProduct = pm.IsFeaturedProduct,
                                                  CollectionId = pm.CollectionId
                                              };

                query_ProductCollection = from pm in query_ProductCollection
                                          where pm.CollectionId == collectionId
                                          orderby pm.DisplayOrder
                                          select pm;

                return Task.FromResult(new PagedList<ProductsCollection>(query_ProductCollection, pageIndex, pageSize));
            });
        }

        /// <summary>
        /// Inserts a product collection mapping
        /// </summary>
        /// <param name="productCollection">Product collection mapping</param>
        /// <param name="productId">Product ident</param>
        public virtual async Task InsertProductCollection(ProductCollection productCollection, string productId)
        {
            if (productCollection == null)
                throw new ArgumentNullException(nameof(productCollection));

            await _productRepository.AddToSet(productId, x => x.ProductCollections, productCollection);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityInserted(productCollection);
        }

        /// <summary>
        /// Updates the product collection mapping
        /// </summary>
        /// <param name="productCollection">Product collection mapping</param>
        /// <param name="productId">Product id</param>
        public virtual async Task UpdateProductCollection(ProductCollection productCollection, string productId)
        {
            if (productCollection == null)
                throw new ArgumentNullException(nameof(productCollection));

            await _productRepository.UpdateToSet(productId, x => x.ProductCollections, z => z.Id, productCollection.Id, productCollection);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityUpdated(productCollection);
        }

        /// <summary>
        /// Deletes a product collection mapping
        /// </summary>
        /// <param name="productCollection">Product collection mapping</param>
        /// <param name="productId">Product id</param>
        public virtual async Task DeleteProductCollection(ProductCollection productCollection, string productId)
        {
            if (productCollection == null)
                throw new ArgumentNullException(nameof(productCollection));

            await _productRepository.PullFilter(productId, x => x.ProductCollections, z => z.Id, productCollection.Id);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.PRODUCTCOLLECTIONS_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(string.Format(CacheKey.PRODUCTS_BY_ID_KEY, productId));

            //event notification
            await _mediator.EntityDeleted(productCollection);
        }

    }
}
